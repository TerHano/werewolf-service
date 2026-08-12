using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Filters;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Models.Request;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.API;

public static class GameEndpoint
{
    public static void RegisterGameEndpoints(this WebApplication app)
    {
        app.MapGet("/api/game/{roomId}/assigned-role",
            async (HttpContext httpContext, GameService gameService, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var assignedRole = await gameService.GetAssignedPlayerRole(roomId, playerGuid);
                return TypedResults.Ok(new APIResponse<RoleName?>()
                {
                    Success = true,
                    Data = assignedRole
                });
            })
            .WithName("GetAssignedRole")
            .WithTags("Game")
            .WithSummary("Get current player's assigned role.")
            .WithDescription("Returns the role assigned to the current player in the specified room.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/my-role",
            async (HttpContext httpContext, GameService gameService, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var myRole = await gameService.GetMyRole(roomId, playerGuid);
                return TypedResults.Ok(new APIResponse<MyRoleDto?>()
                {
                    Success = true,
                    Data = myRole
                });
            })
            .WithName("GetMyRole")
            .WithTags("Game")
            .WithSummary("Get the caller's own card.")
            .WithDescription(
                "Returns the caller's role, whether they are alive, and the player role id their actions are addressed by. Null if they hold no role.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/players",
            async (GameService gameService, string roomId) =>
            {
                var players = await gameService.GetPlayersInGame(roomId);
                return TypedResults.Ok(new APIResponse<List<GamePlayerDto>>()
                {
                    Success = true,
                    Data = players
                });
            })
            .WithName("GetPlayersInGame")
            .WithTags("Game")
            .WithSummary("Get everyone dealt into the game.")
            .WithDescription(
                "Names, avatars and alive/dead by player role id — no roles. Lets a player put names to their own action's target ids.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/all-player-roles",
            async (HttpContext httpContext, GameService gameService, GameAuthorizationService auth, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var allowed = await auth.CanReadAllPlayerRoles(roomId, playerGuid);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                var assignedRoles = await gameService.GetAllAssignedPlayerRolesAndActions(roomId);
                return TypedResults.Ok(new APIResponse<List<PlayerRoleActionDto>>()
                {
                    Success = true,
                    Data = assignedRoles
                });
            })
            .WithName("GetAllPlayerRoles")
            .WithTags("Game")
            .WithSummary("Get all player roles in room.")
            .WithDescription(
                "Returns all assigned player roles and actions. Moderator-run rooms: moderator only. Self-moderated rooms: nobody until the game has been won.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/pack",
            async (HttpContext httpContext, GameService gameService, GameAuthorizationService auth, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var allowed = await auth.CanReadPack(roomId, playerGuid);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                var pack = await gameService.GetWerewolfPack(roomId);
                return TypedResults.Ok(new APIResponse<List<PlayerRoleDTO>>()
                {
                    Success = true,
                    Data = pack
                });
            })
            .WithName("GetWerewolfPack")
            .WithTags("Game")
            .WithSummary("Get the werewolves in this game.")
            .WithDescription("Returns the other werewolves. Werewolves only — the pack has to know each other to act as one.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/{playerRoleId}/role-actions",
            async (HttpContext httpContext, GameService gameService, GameAuthorizationService auth, string roomId,
                int playerRoleId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var allowed = await auth.CanReadPlayerRole(roomId, playerGuid, playerRoleId);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                var state = await gameService.GetActionsForPlayerRole(roomId, playerRoleId);
                return TypedResults.Ok(new APIResponse<List<RoleActionDto>>()
                {
                    Success = true,
                    Data = state
                });
            })
            .WithName("GetRoleActions")
            .WithTags("Game")
            .WithSummary("Get available actions for a player role.")
            .WithDescription(
                "Returns the actions available to a player role. Self-moderated rooms: your own role only.")
            .RequireRoomMembership();

        app.MapPost("/api/game/investigate",
                async (HttpContext httpContext, GameService gameService, GameAuthorizationService auth,
                    InvestigatePlayerRequest request) =>
                {
                    var playerGuid = httpContext.User.GetPlayerId();
                    var allowed = await auth.CanInvestigate(request.RoomId, playerGuid, request);
                    if (!allowed.IsAllowed) return allowed.ToFailure();

                    var investigationResult = await gameService.InvestigatePlayerInRoom(request);
                    return TypedResults.Ok(new APIResponse<InvestigatePlayerResult>()
                    {
                        Success = true,
                        Data = investigationResult
                    });
                })
            .WithName("InvestigatePlayerInRoom")
            .WithTags("Game")
            .WithSummary("Checks if player is a werewolf")
            .WithDescription(
                "Returns true if the player reads as a werewolf. Self-moderated rooms: the living Detective, during their own step.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/{playerRoleId}/queued-action",
            async (HttpContext httpContext, GameService gameService, GameAuthorizationService auth, string roomId,
                int playerRoleId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var allowed = await auth.CanReadPlayerRole(roomId, playerGuid, playerRoleId);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                var state = await gameService.GetPlayerQueuedAction(roomId, playerRoleId);

                return TypedResults.Ok(new APIResponse<PlayerQueuedActionDTO>()
                {
                    Success = true,
                    Data = state!
                });
            })
            .WithName("GetQueuedAction")
            .WithTags("Game")
            .WithSummary("Get queued action for a player.")
            .WithDescription("Returns a player's queued action. Self-moderated rooms: your own only.")
            .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/all-queued-actions",
            async (HttpContext httpContext, GameService gameService, GameAuthorizationService auth, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var allowed = await auth.CanReadAllQueuedActions(roomId, playerGuid);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                var state = await gameService.GetAllQueuedActionsForRoom(roomId);

                return TypedResults.Ok(new APIResponse<List<PlayerQueuedActionDTO>>()
                {
                    Success = true,
                    Data = state
                });
            })
            .WithName("GetAllQueuedActions")
            .WithTags("Game")
            .WithSummary("Get all queued actions in a room.")
            .WithDescription(
                "Moderator-run rooms only — in a self-moderated room this would be the whole night, so no player may read it.")
            .RequireRoomMembership();

        app.MapPost("/api/game/queued-action",
           async (HttpContext httpContext, PlayerActionRequestDTO playerActionRequestDto, GameService gameService,
               GameAuthorizationService auth) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var allowed = await auth.CanSubmitAction(playerActionRequestDto.RoomId, playerGuid,
                    playerActionRequestDto);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                // Store the action against whoever actually sent it, not whoever the request
                // claimed. Matters for the shared werewolf kill, where the ownership check is
                // deliberately relaxed.
                if (allowed.CallerPlayerRoleId.HasValue)
                {
                    playerActionRequestDto.PlayerRoleId = allowed.CallerPlayerRoleId;
                }

                await gameService.QueueActionForPlayer(playerActionRequestDto);
                return TypedResults.Ok(new APIResponse()
                {
                    Success = true,
                });
            })
            .WithName("QueuePlayerAction")
            .WithTags("Game")
            .WithSummary("Queue a player action.")
            .WithDescription(
                "Queues an action. Self-moderated rooms: as yourself, during your own step, and only an action your role may actually take.")
            .RequireRoomMembership();

        // This route carries no room id, so RoomAccessFilter cannot scope it. Resolve the
        // action's room first, then run the same checks by hand.
        app.MapDelete("/api/game/queued-action/{actionId}",
            async (int actionId, HttpContext httpContext, GameService gameService, GameAuthorizationService auth,
                RoomService roomService) =>
            {
                var roomId = await gameService.GetRoomIdForAction(actionId);
                if (roomId == null)
                {
                    return TypedResults.Json(new APIResponse()
                    {
                        Success = false,
                        ErrorMessages = new List<string> { "Action not found." }
                    }, statusCode: StatusCodes.Status404NotFound);
                }

                var playerGuid = httpContext.User.GetPlayerId();
                // Membership cannot be checked by the filter here either, so check it first —
                // otherwise any authenticated caller could probe actions by id.
                if (!await roomService.isPlayerInRoom(roomId, playerGuid))
                {
                    return TypedResults.Json(new APIResponse()
                    {
                        Success = false,
                        ErrorMessages = new List<string> { "You are not a player in this room." }
                    }, statusCode: StatusCodes.Status403Forbidden);
                }

                var allowed = await auth.CanDequeueAction(roomId, playerGuid, actionId);
                if (!allowed.IsAllowed) return allowed.ToFailure();

                await gameService.DequeueActionForPlayer(actionId);
                return TypedResults.Json(new APIResponse()
                {
                    Success = true,
                });
            })
        .WithName("DequeuePlayerAction")
        .WithTags("Game")
        .WithSummary("Remove a queued player action.")
        .WithDescription("Withdraws a queued action. Self-moderated rooms: your own, while its step is still running.")
        .RequireAuthorization();

        app.MapGet("/api/game/{roomId}/night-state",
            async (NightEngineService nightEngine, string roomId) =>
            {
                var state = await nightEngine.GetNightState(roomId);
                return TypedResults.Ok(new APIResponse<NightStateDto>()
                {
                    Success = true,
                    Data = state
                });
            })
            .WithName("GetNightState")
            .WithTags("Game")
            .WithSummary("Get the state of the server-run night call.")
            .WithDescription(
                "Returns which night step is running, when it ends, and the room's running order. Contains no role information.")
            .RequireRoomMembership();

        app.MapPost("/api/game/start-night", async (RoomIdRequest request, NightEngineService nightEngine) =>
            {
                var result = await nightEngine.StartNight(request.RoomId);
                if (result == StartNightResult.Started)
                {
                    return TypedResults.Ok(new APIResponse() { Success = true });
                }

                var message = result switch
                {
                    StartNightResult.NotSelfModerated => "This room runs its night with a human moderator.",
                    StartNightResult.GameNotInProgress => "No game is in progress.",
                    StartNightResult.GameAlreadyWon => "This game is already over.",
                    StartNightResult.NotNight => "It is currently day.",
                    StartNightResult.AlreadyRunning => "The night call has already begun.",
                    StartNightResult.NoStepsConfigured => "This room has no roles that act at night.",
                    _ => "The night could not be started."
                };

                return TypedResults.Ok(new APIResponse()
                {
                    Success = false,
                    ErrorMessages = new List<string> { message }
                });
            })
            .WithName("StartNight")
            .WithTags("Game")
            .WithSummary("Begin the server-run night call.")
            .WithDescription(
                "Starts the first night step. The engine advances the rest on a timer. Self-moderated rooms only.")
            .RequireRoomModerator();

        app.MapPost("/api/game/extend-step", async (RoomIdRequest request,
                IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService) =>
            {
                var (extended, deadline, step) = await gameService.ExtendCurrentNightStep(request.RoomId);
                if (!extended || deadline == null || step == null)
                {
                    return TypedResults.Ok(new APIResponse()
                    {
                        Success = false,
                        ErrorMessages = new List<string> { "There is no night step running to extend." }
                    });
                }

                // Everyone's countdown has to agree, so this goes to the whole room. It reveals
                // only that the step was extended, which the table can see anyway.
                await hubContext.Clients.Group(request.RoomId.ToUpper())
                    .StepExtended(step.Value, deadline.Value);

                return TypedResults.Ok(new APIResponse() { Success = true });
            })
            .WithName("ExtendNightStep")
            .WithTags("Game")
            .WithSummary("Give the current night step more time.")
            .WithDescription(
                "Adds one step's worth of time to the running step, for a player who is still choosing. Badge holder only. There is deliberately no way to shorten a step.")
            .RequireRoomModerator();

        app.MapPost("/api/game/end-night", async (PlayerIdAndRoomIdRequestDto request,
            IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService,
            RoomService roomService) =>
        {
            // In a self-moderated room the engine owns night resolution. Letting this through
            // as well would resolve the same queued actions twice.
            var settings = await roomService.GetRoleSettingsForRoom(request.RoomId);
            if (settings.SelfModerated)
            {
                return TypedResults.Ok(new APIResponse()
                {
                    Success = false,
                    ErrorMessages = new List<string> { "This room's night is run by the server." }
                });
            }

            await gameService.EndNight(request.RoomId);
            var winCondition = await gameService.CheckWinCondition(request.RoomId);
            if (winCondition != WinCondition.None)
            {
                await hubContext.Clients.Group(request.RoomId).WinConditionMet();
            }

            await hubContext.Clients.Group(request.RoomId).DayTimeUpdated();

            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("EndNight")
        .WithTags("Game")
        .WithSummary("End the night phase.")
        .WithDescription("Processes all queued night actions and transitions to day phase. Checks for win conditions.")
        .RequireRoomModerator();

        app.MapPost("/api/game/vote-out-player", async (PlayerVoteOutRequestDTO request,
            IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService) =>
        {
            var lynched = await gameService.LynchChosenPlayer(request.RoomId, request.PlayerRoleId);
            if (!lynched)
            {
                return TypedResults.Ok(new APIResponse()
                {
                    Success = false,
                    ErrorMessages = new List<string> { "The village only votes during the day." }
                });
            }

            // A day-0 lynch counts as the game's first death just as a night kill does, so the
            // badge can change hands here too.
            var newModerator = await gameService.AssignModeratorBadgeIfFirstDeath(request.RoomId);
            if (newModerator != null)
            {
                await hubContext.Clients.Group(request.RoomId.ToUpper()).ModeratorUpdated(newModerator);
                await hubContext.Clients.Group(request.RoomId.ToUpper()).PlayersInLobbyUpdated();
            }

            var winCondition = await gameService.CheckWinCondition(request.RoomId);
            if (winCondition != WinCondition.None)
            {
                await hubContext.Clients.Group(request.RoomId).WinConditionMet();
            }

            await hubContext.Clients.Group(request.RoomId.ToUpper()).DayTimeUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("VoteOutPlayer")
        .WithTags("Game")
        .WithSummary("Vote out a player.")
        .WithDescription("Removes a player from the game through village voting. Checks for win conditions.")
        .RequireRoomModerator();

        app.MapGet("/api/game/{roomId}/day-time",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetCurrentNightAndTime(roomId);

                return TypedResults.Ok(new APIResponse<DayDto>()
                {
                    Success = true,
                    Data = state
                });
            })
        .WithName("GetDayTime")
        .WithTags("Game")
        .WithSummary("Get current day and time information.")
        .WithDescription("Returns the current day number and phase (day/night) for the game.")
        .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/latest-deaths",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetLatestDeaths(roomId);

                return TypedResults.Ok(new APIResponse<List<PlayerDTO>>()
                {
                    Success = true,
                    Data = state
                });
            })
        .WithName("GetLatestDeaths")
        .WithTags("Game")
        .WithSummary("Get latest player deaths.")
        .WithDescription("Returns a list of players who died in the most recent night/day phase.")
        .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/check-win-condition",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetWinConditionForRoom(roomId);

                return TypedResults.Ok(new APIResponse<WinCondition>()
                {
                    Success = true,
                    Data = state
                });
            })
        .WithName("CheckWinCondition")
        .WithTags("Game")
        .WithSummary("Check for game win condition.")
        .WithDescription("Returns the current win condition status for the game, if any faction has won.")
        .RequireRoomMembership();

        app.MapGet("/api/game/{roomId}/summary",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetGameSummary(roomId);

                return TypedResults.Ok(new APIResponse<List<GameNightHistoryDTO>>()
                {
                    Success = true,
                    Data = state
                });
            })
        .WithName("GetGameSummary")
        .WithTags("Game")
        .WithSummary("Get game summary.")
        .WithDescription("Returns a historical summary of game events, organized by night/day.")
        .RequireRoomMembership();
    }
}
