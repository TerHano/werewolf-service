using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Repository;
using WerewolfParty_Server.Role;

namespace WerewolfParty_Server.Service;

/// <summary>
/// Decides who may read and submit what during a game.
///
/// Before self-moderation, every game endpoint was moderator-only, and that single check was
/// what kept the game secret: one trusted caller, everyone else locked out. Self-moderated
/// rooms have no such caller — every player posts their own action — so that guarantee has to
/// be rebuilt out of per-player checks, and the checks have to be exact. This is the most
/// security-sensitive code in the app.
///
/// Rooms running the classic human-moderator flow keep the old rule unchanged: the moderator
/// may do everything, nobody else may do anything. Each method below branches on that first.
/// </summary>
public class GameAuthorizationService(
    RoomRepository roomRepository,
    RoleSettingsRepository roleSettingsRepository,
    PlayerRoleRepository playerRoleRepository,
    RoomGameActionRepository roomGameActionRepository,
    GameService gameService,
    RoomService roomService)
{
    /// <summary>
    /// May the caller read this player role's available actions, or its queued action? Only
    /// ever their own — the action list names valid targets and would otherwise map the game.
    /// </summary>
    public async Task<AuthorizationResult> CanReadPlayerRole(string roomId, Guid playerGuid, int playerRoleId)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        if (!settings.SelfModerated) return await RequireModerator(roomId, playerGuid);

        var callerRole = await TryGetCallerRole(roomId, playerGuid);
        if (callerRole == null) return AuthorizationResult.Forbidden("You do not hold a role in this game.");

        return callerRole.Id == playerRoleId
            ? AuthorizationResult.Allowed()
            : AuthorizationResult.Forbidden("You may only read your own role.");
    }

    /// <summary>
    /// May the caller see every player's role? During play, nobody may — that is the whole
    /// game. Once a faction has won it is public, and the end-of-game summary needs it.
    /// </summary>
    public async Task<AuthorizationResult> CanReadAllPlayerRoles(string roomId, Guid playerGuid)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        if (!settings.SelfModerated) return await RequireModerator(roomId, playerGuid);

        var room = await roomRepository.GetRoom(roomId);
        return room.WinCondition != WinCondition.None
            ? AuthorizationResult.Allowed()
            : AuthorizationResult.Forbidden("Roles are revealed when the game ends.");
    }

    /// <summary>
    /// May the caller see every queued action in the room? In a self-moderated room this is
    /// the entire night — who the wolves picked, who the doctor saved — so no player may read
    /// it, ever.
    /// </summary>
    public async Task<AuthorizationResult> CanReadAllQueuedActions(string roomId, Guid playerGuid)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        return settings.SelfModerated
            ? AuthorizationResult.Forbidden("Queued actions are private in a self-moderated room.")
            : await RequireModerator(roomId, playerGuid);
    }

    /// <summary>
    /// May the caller queue this action? The strictest check in the app, because a hole here
    /// lets a Villager heal, or a Witch spend a used-up potion.
    /// </summary>
    public async Task<AuthorizationResult> CanSubmitAction(string roomId, Guid playerGuid,
        PlayerActionRequestDTO request)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        if (!settings.SelfModerated) return await RequireModerator(roomId, playerGuid);

        var callerRole = await TryGetCallerRole(roomId, playerGuid);
        if (callerRole == null) return AuthorizationResult.Forbidden("You do not hold a role in this game.");
        if (!callerRole.IsAlive) return AuthorizationResult.Forbidden("Dead players cannot act.");

        // Werewolves share one queued kill for the whole pack, so the request carries no
        // player id. Any living wolf may write or overwrite it. Scoped tightly to this one
        // action type so it does not become a general hole.
        var isSharedWerewolfKill = request.Action == ActionType.WerewolfKill;
        if (!isSharedWerewolfKill && request.PlayerRoleId != callerRole.Id)
        {
            return AuthorizationResult.Forbidden("You may only submit actions as yourself.");
        }

        var room = await roomRepository.GetRoom(roomId);
        var expectedStep = NightStepRoles.StepForRole(callerRole.Role);
        if (expectedStep == null) return AuthorizationResult.Forbidden("Your role has no night action.");
        if (room.NightStep != expectedStep)
        {
            return AuthorizationResult.Forbidden("It is not your turn.");
        }

        var valid = await ValidateAgainstRoleRules(roomId, callerRole, request.Action, request.AffectedPlayerRoleId);
        if (!valid.IsAllowed) return valid;

        // Hand back who the caller actually is, so the endpoint can stamp the stored action
        // with it. The shared werewolf kill is exempt from the ownership check above, which
        // would otherwise let a wolf write someone else's id into the row and misattribute the
        // kill in the end-of-game summary.
        return AuthorizationResult.Allowed(callerRole.Id);
    }

    /// <summary>
    /// May the caller withdraw this queued action? Only their own, and only while the step it
    /// belongs to is still running — once the night has moved on, the choice is made.
    /// </summary>
    public async Task<AuthorizationResult> CanDequeueAction(string roomId, Guid playerGuid, int actionId)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        if (!settings.SelfModerated) return await RequireModerator(roomId, playerGuid);

        var action = await roomGameActionRepository.GetActionById(actionId);
        if (action == null) return AuthorizationResult.NotFound("Action not found.");

        var callerRole = await TryGetCallerRole(roomId, playerGuid);
        if (callerRole == null) return AuthorizationResult.Forbidden("You do not hold a role in this game.");
        if (!callerRole.IsAlive) return AuthorizationResult.Forbidden("Dead players cannot act.");

        var isSharedWerewolfKill = action.Action == ActionType.WerewolfKill;
        if (isSharedWerewolfKill)
        {
            if (callerRole.Role != RoleName.WereWolf)
            {
                return AuthorizationResult.Forbidden("That action is not yours.");
            }
        }
        else if (action.PlayerRoleId != callerRole.Id)
        {
            return AuthorizationResult.Forbidden("That action is not yours.");
        }

        var room = await roomRepository.GetRoom(roomId);
        var expectedStep = NightStepRoles.StepForRole(callerRole.Role);
        if (room.NightStep != expectedStep)
        {
            return AuthorizationResult.Forbidden("That step of the night is over.");
        }

        return AuthorizationResult.Allowed();
    }

    /// <summary>
    /// May the caller investigate? The Detective only, during their own step, and never
    /// themselves. The answer goes back to the caller alone.
    /// </summary>
    public async Task<AuthorizationResult> CanInvestigate(string roomId, Guid playerGuid,
        InvestigatePlayerRequest request)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        if (!settings.SelfModerated) return await RequireModerator(roomId, playerGuid);

        var callerRole = await TryGetCallerRole(roomId, playerGuid);
        if (callerRole == null) return AuthorizationResult.Forbidden("You do not hold a role in this game.");
        if (callerRole.Role != RoleName.Detective)
        {
            return AuthorizationResult.Forbidden("Only the Detective can investigate.");
        }

        if (!callerRole.IsAlive) return AuthorizationResult.Forbidden("Dead players cannot act.");

        var room = await roomRepository.GetRoom(roomId);
        if (room.NightStep != NightStep.DetectiveInvestigate)
        {
            return AuthorizationResult.Forbidden("It is not your turn.");
        }

        // request.PlayerRoleId is the person being investigated, not the caller.
        return await ValidateAgainstRoleRules(roomId, callerRole, ActionType.Investigate, request.PlayerRoleId);
    }

    /// <summary>
    /// May the caller see the rest of the pack? Living werewolves only — the wolves have to
    /// know each other to act as one, and nobody else may.
    /// </summary>
    public async Task<AuthorizationResult> CanReadPack(string roomId, Guid playerGuid)
    {
        var callerRole = await TryGetCallerRole(roomId, playerGuid);
        if (callerRole == null) return AuthorizationResult.Forbidden("You do not hold a role in this game.");

        return callerRole.Role == RoleName.WereWolf
            ? AuthorizationResult.Allowed()
            : AuthorizationResult.Forbidden("You are not a werewolf.");
    }

    /// <summary>
    /// The caller's own role row, or null if they are in the room but hold no dealt role — a
    /// spectator who joined after the cards went out.
    /// </summary>
    public async Task<PlayerRoleEntity?> TryGetCallerRole(string roomId, Guid playerGuid)
    {
        try
        {
            return await playerRoleRepository.GetPlayerRoleInRoomUsingPlayerGuid(roomId, playerGuid);
        }
        catch (PlayerNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// The final check, and the one that matters most: the server already knows exactly what
    /// this player may do right now — <see cref="Role.Role.GetActions"/> returns it, complete
    /// with the reason an ability is unavailable and the list of legal targets. Ask it, rather
    /// than trusting what the client sent.
    /// </summary>
    private async Task<AuthorizationResult> ValidateAgainstRoleRules(string roomId, PlayerRoleEntity callerRole,
        ActionType action, int affectedPlayerRoleId)
    {
        var availableActions = await gameService.GetActionsForPlayerRole(roomId, callerRole.Id);
        var match = availableActions.FirstOrDefault(available => available.Type == action);

        if (match == null)
        {
            return AuthorizationResult.Forbidden("Your role cannot take that action.");
        }

        if (!match.Enabled)
        {
            return AuthorizationResult.Forbidden(match.DisabledReason ?? "That action is not available.");
        }

        if (!match.ValidPlayerIds.Contains(affectedPlayerRoleId))
        {
            return AuthorizationResult.Forbidden("That is not a valid target.");
        }

        return AuthorizationResult.Allowed();
    }

    private async Task<AuthorizationResult> RequireModerator(string roomId, Guid playerGuid)
    {
        return await roomService.IsPlayerModeratorOfRoom(roomId, playerGuid)
            ? AuthorizationResult.Allowed()
            : AuthorizationResult.Forbidden("You are not the moderator of this room.");
    }
}

public class AuthorizationResult
{
    public bool IsAllowed { get; private init; }
    public string? Reason { get; private init; }
    public int StatusCode { get; private init; }

    /// <summary>
    /// The caller's own player role id, when the check established one. Null in
    /// moderator-run rooms, where the moderator acts on behalf of others.
    /// </summary>
    public int? CallerPlayerRoleId { get; private init; }

    public static AuthorizationResult Allowed(int? callerPlayerRoleId = null) =>
        new() { IsAllowed = true, StatusCode = StatusCodes.Status200OK, CallerPlayerRoleId = callerPlayerRoleId };

    public static AuthorizationResult Forbidden(string reason) =>
        new() { IsAllowed = false, Reason = reason, StatusCode = StatusCodes.Status403Forbidden };

    public static AuthorizationResult NotFound(string reason) =>
        new() { IsAllowed = false, Reason = reason, StatusCode = StatusCodes.Status404NotFound };

    /// <summary>The standard refusal body, so every endpoint fails the same shape.</summary>
    public IResult ToFailure() => TypedResults.Json(new APIResponse
    {
        Success = false,
        ErrorMessages = new List<string> { Reason ?? "Not permitted." }
    }, statusCode: StatusCode);
}
