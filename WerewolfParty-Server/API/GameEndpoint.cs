using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Hubs;
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
            .RequireAuthorization();

        app.MapGet("/api/game/{roomId}/all-player-roles",
            async (HttpContext httpContext, GameService gameService, RoomService roomService, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var currentPlayer = await roomService.GetPlayerInRoomUsingGuid(roomId, playerGuid);
                var currentModerator = await roomService.GetModeratorForRoom(roomId);
                if (currentPlayer.Id != currentModerator?.Id)
                {
                    throw new Exception("You are not the moderator of this room.");
                }

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
            .WithDescription("Returns all assigned player roles and actions in the room. Only accessible by the moderator.")
            .RequireAuthorization();

        app.MapGet("/api/game/{roomId}/{playerRoleId}/role-actions",
            async (GameService gameService, string roomId, int playerRoleId) =>
            {
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
            .WithDescription("Returns a list of available actions for the specified player role in a room.")
            .RequireAuthorization();

        app.MapGet("/api/game/{roomId}/{playerRoleId}/queued-action",
            async (GameService gameService, string roomId, int playerRoleId) =>
            {
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
            .WithDescription("Returns the currently queued action for the specified player role.")
            .RequireAuthorization();

        app.MapGet("/api/game/{roomId}/all-queued-actions",
            async (GameService gameService, string roomId) =>
            {
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
            .WithDescription("Returns a list of all queued actions from players in the specified room.")
            .RequireAuthorization();

        app.MapPost("/api/game/queued-action",
           async (PlayerActionRequestDTO playerActionRequestDto, GameService gameService) =>
            {
                await gameService.QueueActionForPlayer(playerActionRequestDto);
                return TypedResults.Ok(new APIResponse()
                {
                    Success = true,
                });
            })
            .WithName("QueuePlayerAction")
            .WithTags("Game")
            .WithSummary("Queue a player action.")
            .WithDescription("Creates a queued action for a player role in the game.")
            .RequireAuthorization();

        app.MapDelete("/api/game/queued-action/{actionId}", async (int actionId, GameService gameService) =>
        {
            await gameService.DequeueActionForPlayer(actionId);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("DequeuePlayerAction")
        .WithTags("Game")
        .WithSummary("Remove a queued player action.")
        .WithDescription("Deletes a previously queued action for a player.")
        .RequireAuthorization();

        app.MapPost("/api/game/end-night", async (PlayerIdAndRoomIdRequestDto request,
            IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService) =>
        {
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
        .RequireAuthorization();

        app.MapPost("/api/game/vote-out-player", async (PlayerVoteOutRequestDTO request,
            IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService) =>
        {
            await gameService.LynchChosenPlayer(request.RoomId, request.PlayerRoleId);
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
        .RequireAuthorization();

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
        .RequireAuthorization();

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
        .RequireAuthorization();

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
        .RequireAuthorization();

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
        .RequireAuthorization();
    }
}
