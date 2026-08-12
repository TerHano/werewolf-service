using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Repository;
using WerewolfParty_Server.Role;

namespace WerewolfParty_Server.Service;

/// <summary>
/// Runs the night call for self-moderated rooms, in place of a human moderator.
///
/// Two rules shape everything here, and both exist to stop the shape of the night leaking who
/// is still alive:
///
/// <list type="number">
/// <item>Every role in the deck gets a step every night, whether or not anyone can act in it.
/// A missing Doctor step would announce that the Doctor is dead.</item>
/// <item>A step always runs for its full duration. Nothing shortens it — not everyone having
/// submitted, not an empty step, not a human. A step that ends in four seconds says as much as
/// one that never happened.</item>
/// </list>
///
/// Consequently the deadline is the <i>only</i> thing that advances the night, which leaves a
/// single advance path rather than several racing each other.
/// </summary>
public class NightEngineService(
    RoomRepository roomRepository,
    RoleSettingsRepository roleSettingsRepository,
    PlayerRoleRepository playerRoleRepository,
    GameService gameService,
    IServiceScopeFactory scopeFactory,
    IHubContext<EventsHub, IClientEventsHub> hubContext,
    ILogger<NightEngineService> logger)
{
    /// <summary>
    /// The steps this room runs, in order. Derived from the deck the room was configured with —
    /// deliberately not from who is currently alive, so the running order is identical on every
    /// night of the game.
    /// </summary>
    private static List<NightStep> GetStepsForRoom(RoomSettingsEntity settings)
    {
        return NightStepRoles.All
            .Where(stepRole => stepRole.Role == RoleName.WereWolf
                ? settings.NumberOfWerewolves > 0
                : settings.SelectedRoles.Contains(stepRole.Role))
            .Select(stepRole => stepRole.Step)
            .ToList();
    }

    /// <summary>
    /// The public state of the night call, safe for any player in the room to read.
    /// </summary>
    public async Task<NightStateDto> GetNightState(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        return new NightStateDto
        {
            SelfModerated = settings.SelfModerated,
            CurrentStep = room.NightStep,
            StepDeadline = room.NightStepDeadline,
            CurrentNight = room.CurrentNight,
            IsDay = room.isDay,
            Steps = settings.SelfModerated ? GetStepsForRoom(settings) : []
        };
    }

    /// <summary>
    /// Begins the night call. Deliberately explicit rather than automatic on dealing or on the
    /// day ending: the table needs a moment to look at their cards and to finish arguing, and
    /// somebody has to decide when that moment is over.
    /// </summary>
    public async Task<StartNightResult> StartNight(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        if (!settings.SelfModerated) return StartNightResult.NotSelfModerated;
        if (room.GameState != GameState.CardsDealt) return StartNightResult.GameNotInProgress;
        if (room.WinCondition != WinCondition.None) return StartNightResult.GameAlreadyWon;
        if (room.isDay) return StartNightResult.NotNight;
        if (room.NightStep != null) return StartNightResult.AlreadyRunning;

        var steps = GetStepsForRoom(settings);
        if (steps.Count == 0) return StartNightResult.NoStepsConfigured;

        var deadline = DateTime.UtcNow.AddSeconds(settings.NightStepSeconds);

        // Guarded like every other transition: if two people press "begin" together, only one
        // of them starts the night.
        var started = await roomRepository.TryMoveToNightStep(roomId, null, steps[0], deadline);
        if (!started) return StartNightResult.AlreadyRunning;

        var group = roomId.ToUpper();
        await hubContext.Clients.Group(group).NightStarted(room.CurrentNight);
        await hubContext.Clients.Group(group).NightStepChanged(steps[0], deadline);
        await NotifyActingPlayers(roomId, steps[0]);
        return StartNightResult.Started;
    }

    /// <summary>
    /// Advances every room whose current step has run out of time. Called by
    /// <see cref="NightClockService"/>.
    /// </summary>
    public async Task AdvanceExpiredRooms()
    {
        var expiredRoomIds = await roomRepository.GetRoomIdsWithExpiredNightStep(DateTime.UtcNow);
        foreach (var roomId in expiredRoomIds)
        {
            try
            {
                await AdvanceRoom(roomId);
            }
            catch (Exception e)
            {
                // One wedged room must not stop the clock for every other room, and the next
                // tick will try again in a second.
                logger.LogError(e, "Failed to advance night for room {RoomId}", roomId);
            }
        }
    }

    private async Task AdvanceRoom(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        var currentStep = room.NightStep;
        if (currentStep == null) return;

        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        var steps = GetStepsForRoom(settings);

        var currentIndex = steps.IndexOf(currentStep.Value);
        var nextStep = currentIndex >= 0 && currentIndex + 1 < steps.Count
            ? steps[currentIndex + 1]
            : (NightStep?)null;

        var group = roomId.ToUpper();

        if (nextStep != null)
        {
            var deadline = DateTime.UtcNow.AddSeconds(settings.NightStepSeconds);
            var moved = await roomRepository.TryMoveToNightStep(roomId, currentStep, nextStep, deadline);
            if (!moved) return;

            await hubContext.Clients.Group(group).NightStepChanged(nextStep.Value, deadline);
            await NotifyActingPlayers(roomId, nextStep.Value);
            return;
        }

        // Last step is over. Claim the resolve by moving into Resolving; whoever wins that
        // move is the one that applies the queued actions.
        //
        // Resolving carries a null deadline deliberately: the work queue only picks up rooms
        // that have one, so a room being resolved cannot be picked up again by the next tick
        // while resolution is still in flight.
        var claimed = await roomRepository.TryMoveToNightStep(roomId, currentStep, NightStep.Resolving, null);
        if (!claimed) return;

        await ResolveNight(roomId, group);
    }

    /// <summary>
    /// Tells the living players who act in this step that it is their turn — and nobody else.
    /// An empty step notifies no one, which is exactly why the step still has to run its full
    /// length: silence is the only thing that distinguishes it.
    /// </summary>
    private async Task NotifyActingPlayers(string roomId, NightStep step)
    {
        var actingRole = NightStepRoles.All.FirstOrDefault(entry => entry.Step == step).Role;
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);

        var actingPlayers = playerRoles
            .Where(playerRole => playerRole.Role == actingRole && playerRole.IsAlive)
            .ToList();

        foreach (var playerRole in actingPlayers)
        {
            // Must match NameUserIdProvider's canonical lowercase form, or this reaches nobody
            // and does so silently.
            var userId = playerRole.PlayerRoom.PlayerId.ToString();
            await hubContext.Clients.User(userId).YourTurn(step);
        }

        if (actingPlayers.Count == 0) return;

        // Buzz the same people on their phones. Deliberately not awaited: a slow or unreachable
        // push service must not hold up the night, and the in-app prompt above has already
        // gone out. The notification says only that it is their turn — never which role, who
        // they may target, or what anyone chose.
        var playerIds = actingPlayers.Select(playerRole => playerRole.PlayerRoom.PlayerId).ToList();
        _ = Task.Run(async () =>
        {
            try
            {
                // Runs outside the request scope, so it needs its own PushService and DbContext
                // rather than borrowing ones that are about to be disposed.
                using var scope = scopeFactory.CreateScope();
                var pushService = scope.ServiceProvider.GetRequiredService<PushService>();
                await pushService.NotifyPlayers(playerIds, roomId, "Werewolf Party", "It's your turn.");
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to send turn notifications for room {RoomId}", roomId);
            }
        });
    }

    private async Task ResolveNight(string roomId, string group)
    {
        // EndNight is the existing resolution: it applies the queued actions with all the
        // established rules (revive beats kill, Vigilante guilt, Suicide) and moves the room
        // into day. The engine deliberately does not reimplement any of that.
        await gameService.EndNight(roomId);

        // Out of Resolving and back to "no night call in progress".
        await roomRepository.TryMoveToNightStep(roomId, NightStep.Resolving, null, null);

        await hubContext.Clients.Group(group).NightResolved();

        // If this night produced the game's first death, that player takes the moderator badge.
        var newModerator = await gameService.AssignModeratorBadgeIfFirstDeath(roomId);
        if (newModerator != null)
        {
            await hubContext.Clients.Group(group).ModeratorUpdated(newModerator);
            await hubContext.Clients.Group(group).PlayersInLobbyUpdated();
        }

        var winCondition = await gameService.CheckWinCondition(roomId);
        if (winCondition != WinCondition.None)
        {
            await hubContext.Clients.Group(group).WinConditionMet();
        }

        await hubContext.Clients.Group(group).DayTimeUpdated();
    }
}

public enum StartNightResult
{
    Started,
    NotSelfModerated,
    GameNotInProgress,
    GameAlreadyWon,
    NotNight,
    AlreadyRunning,
    NoStepsConfigured
}
