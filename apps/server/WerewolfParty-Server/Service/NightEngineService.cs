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
/// The night is <b>opaque</b>: which step is running is told only to the players who act in it.
/// Everyone else sees that a night is in progress and nothing more. That is what makes it safe
/// for a step to end as soon as its actors have locked in.
///
/// The rules that keep it from leaking:
///
/// <list type="number">
/// <item>Every role in the deck gets a step every night, whether or not anyone can act in it.
/// A missing Doctor step would announce that the Doctor is dead.</item>
/// <item>A step nobody can act in still takes a plausible amount of time — a random slice of
/// the configured length, rather than always running to the full deadline. If empty steps were
/// reliably the longest, the length of the night would still count the dead.</item>
/// <item>No step name, deadline or position is ever broadcast to the room. Only
/// <see cref="IClientEventsHub.YourTurn"/>, which is sent to the acting players alone, carries
/// any of it.</item>
/// </list>
///
/// An earlier version ran every step for a fixed full length and broadcast the step name and a
/// countdown to everyone. That was safe but made a four-role night about three minutes of
/// sitting in the dark; hiding the step is what buys the time back.
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
    /// The night as this particular caller is allowed to see it.
    ///
    /// The step and its deadline are filled in <b>only</b> for a player who acts in the step
    /// that is running. To everyone else the night is opaque: they learn that it is under way
    /// and nothing else. Without that, a step ending early would tell the room that somebody
    /// acted — and a step running to its deadline would tell them nobody did, which is to say
    /// that the role is dead.
    /// </summary>
    public async Task<NightStateDto> GetNightState(string roomId, Guid playerGuid)
    {
        var room = await roomRepository.GetRoom(roomId);
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        var state = new NightStateDto
        {
            SelfModerated = settings.SelfModerated,
            CurrentNight = room.CurrentNight,
            IsDay = room.isDay,
            IsNightCallRunning = room.NightStep != null
        };

        if (room.NightStep == null || room.NightStep == NightStep.Resolving) return state;

        var callerRole = await TryGetActingRole(roomId, playerGuid, room.NightStep.Value);
        if (callerRole == null) return state;

        state.CurrentStep = room.NightStep;
        state.StepDeadline = room.NightStepDeadline;
        state.HasLockedIn = room.NightStepLockedIn.Contains(callerRole.Id);
        return state;
    }

    /// <summary>
    /// The caller's role, but only when they are alive and act in the given step. Null covers
    /// everyone else — spectators, the dead, and players whose turn it simply is not.
    /// </summary>
    private async Task<PlayerRoleEntity?> TryGetActingRole(string roomId, Guid playerGuid, NightStep step)
    {
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var callerRole = playerRoles.FirstOrDefault(playerRole =>
            playerRole.PlayerRoom.PlayerId == playerGuid);

        if (callerRole == null || !callerRole.IsAlive) return null;
        return NightStepRoles.StepForRole(callerRole.Role) == step ? callerRole : null;
    }

    /// <summary>
    /// Locks the caller in for the current step. Once every living player who acts in the step
    /// has locked in, the step ends immediately rather than waiting out its deadline.
    ///
    /// Allowed whether or not they queued anything — a Witch who wants to save both her
    /// potions still needs a way to say she is done.
    /// </summary>
    public async Task<bool> LockIn(string roomId, Guid playerGuid)
    {
        var room = await roomRepository.GetRoom(roomId);
        if (room.NightStep == null || room.NightStep == NightStep.Resolving) return false;

        var step = room.NightStep.Value;
        var callerRole = await TryGetActingRole(roomId, playerGuid, step);
        if (callerRole == null) return false;

        await roomRepository.TryAddLockIn(roomId, step, callerRole.Id);

        // Advance as soon as nobody is left to hear from. Re-read rather than trusting the copy
        // above, since another actor may have locked in at the same moment.
        var refreshed = await roomRepository.GetRoom(roomId);
        if (refreshed.NightStep != step) return true;

        var actors = await GetLivingActors(roomId, step);
        if (actors.All(actor => refreshed.NightStepLockedIn.Contains(actor.Id)))
        {
            await AdvanceRoom(roomId);
        }

        return true;
    }

    /// <summary>
    /// Tells the acting players their step now ends later. Not broadcast: they are the only
    /// ones with a countdown, and the only ones allowed to know which step is running.
    /// </summary>
    public async Task NotifyStepExtended(string roomId, NightStep step, DateTime deadline)
    {
        var actors = await GetLivingActors(roomId, step);
        foreach (var actor in actors)
        {
            var userId = actor.PlayerRoom.PlayerId.ToString();
            await hubContext.Clients.User(userId).StepExtended(step, deadline);
        }
    }

    private async Task<List<PlayerRoleEntity>> GetLivingActors(string roomId, NightStep step)
    {
        var actingRole = NightStepRoles.All.FirstOrDefault(entry => entry.Step == step).Role;
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        return playerRoles
            .Where(playerRole => playerRole.Role == actingRole && playerRole.IsAlive)
            .ToList();
    }

    /// <summary>
    /// How long a step should last.
    ///
    /// A step somebody can act in gets the configured length. A step nobody can act in gets a
    /// random slice of it instead — long enough to be plausible, never reliably the longest.
    /// If empty steps always ran to the full deadline, the total length of the night would
    /// still count how many roles had died.
    /// </summary>
    private static int StepSeconds(RoomSettingsEntity settings, bool hasLivingActor)
    {
        if (hasLivingActor) return settings.NightStepSeconds;

        var shortest = Math.Max(3, settings.NightStepSeconds * 2 / 5);
        return Random.Shared.Next(shortest, settings.NightStepSeconds + 1);
    }

    private async Task<DateTime> DeadlineFor(string roomId, RoomSettingsEntity settings, NightStep step)
    {
        var actors = await GetLivingActors(roomId, step);
        return DateTime.UtcNow.AddSeconds(StepSeconds(settings, actors.Count > 0));
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

        var deadline = await DeadlineFor(roomId, settings, steps[0]);

        // Guarded like every other transition: if two people press "begin" together, only one
        // of them starts the night.
        var started = await roomRepository.TryMoveToNightStep(roomId, null, steps[0], deadline);
        if (!started) return StartNightResult.AlreadyRunning;

        var group = roomId.ToUpper();
        await hubContext.Clients.Group(group).NightStarted(room.CurrentNight);
        await NotifyActingPlayers(roomId, steps[0], deadline);
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
            var deadline = await DeadlineFor(roomId, settings, nextStep.Value);
            var moved = await roomRepository.TryMoveToNightStep(roomId, currentStep, nextStep, deadline);
            if (!moved) return;

            await NotifyActingPlayers(roomId, nextStep.Value, deadline);
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
    ///
    /// This is the only place the step is named to anyone. The room gets a bare
    /// <see cref="IClientEventsHub.NightAdvanced"/> so clients can refresh, carrying no step,
    /// no deadline and no position in the order.
    /// </summary>
    private async Task NotifyActingPlayers(string roomId, NightStep step, DateTime deadline)
    {
        var actingPlayers = await GetLivingActors(roomId, step);

        await hubContext.Clients.Group(roomId.ToUpper()).NightAdvanced();

        foreach (var playerRole in actingPlayers)
        {
            // Must match NameUserIdProvider's canonical lowercase form, or this reaches nobody
            // and does so silently.
            var userId = playerRole.PlayerRoom.PlayerId.ToString();
            await hubContext.Clients.User(userId).YourTurn(step, deadline);
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
