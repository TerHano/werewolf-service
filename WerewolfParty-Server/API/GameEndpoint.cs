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
            (HttpContext httpContext, GameService gameService, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var assignedRole = gameService.GetAssignedPlayerRole(roomId, playerGuid);
                return TypedResults.Ok(new APIResponse<RoleName?>()
                {
                    Success = true,
                    Data = assignedRole
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/all-player-roles",
            (HttpContext httpContext, GameService gameService, RoomService roomService, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var currentPlayer = roomService.GetPlayerInRoomUsingGuid(roomId, playerGuid);
                var currentModerator = roomService.GetModeratorForRoom(roomId);
                if (currentPlayer.Id != currentModerator.Id)
                {
                    throw new Exception("You are not the moderator of this room.");
                }

                var assignedRoles = gameService.GetAllAssignedPlayerRolesAndActions(roomId);
                return TypedResults.Ok(new APIResponse<List<PlayerRoleActionDto>>()
                {
                    Success = true,
                    Data = assignedRoles
                });
            }).RequireAuthorization();


        app.MapGet("/api/game/{roomId}/{playerRoleId}/role-actions",
            (HttpContext httpContext, GameService gameService, string roomId, int playerRoleId) =>
            {
                var state = gameService.GetActionsForPlayerRole(roomId, playerRoleId);
                return TypedResults.Ok(new APIResponse<List<RoleActionDto>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/{playerRoleId}/queued-action",
            (HttpContext httpContext, GameService gameService, string roomId, int playerRoleId) =>
            {
                var state = gameService.GetPlayerQueuedAction(roomId, playerRoleId);

                return TypedResults.Ok(new APIResponse<PlayerQueuedActionDTO>()
                {
                    Success = true,
                    Data = state!
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/all-queued-actions",
            (HttpContext httpContext, GameService gameService, string roomId) =>
            {
                var state = gameService.GetAllQueuedActionsForRoom(roomId);

                return TypedResults.Ok(new APIResponse<List<PlayerQueuedActionDTO>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapPost("/api/game/queued-action",
            (PlayerActionRequestDTO playerActionRequestDto, GameService gameService) =>
            {
                gameService.QueueActionForPlayer(playerActionRequestDto);
                return TypedResults.Ok(new APIResponse()
                {
                    Success = true,
                });
            }).RequireAuthorization();

        app.MapDelete("/api/game/queued-action/{actionId}", (int actionId, GameService gameService) =>
        {
            gameService.DequeueActionForPlayer(actionId);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapPost("/api/game/end-night", (PlayerIdAndRoomIdRequestDto request,
            IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService) =>
        {
            gameService.EndNight(request.RoomId);
            var winCondition = gameService.CheckWinCondition(request.RoomId);
            if (winCondition != WinCondition.None)
            {
                hubContext.Clients.Group(request.RoomId).WinConditionMet();
            }

            hubContext.Clients.Group(request.RoomId).DayTimeUpdated();

            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapPost("/api/game/vote-out-player", (PlayerVoteOutRequestDTO request,
            IHubContext<EventsHub, IClientEventsHub> hubContext, GameService gameService) =>
        {
            gameService.LynchChosenPlayer(request.RoomId, request.PlayerRoleId);
            var winCondition = gameService.CheckWinCondition(request.RoomId);
            if (winCondition != WinCondition.None)
            {
                hubContext.Clients.Group(request.RoomId).WinConditionMet();
            }

            hubContext.Clients.Group(request.RoomId.ToUpper()).DayTimeUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/day-time",
            (GameService gameService, string roomId) =>
            {
                var state = gameService.GetCurrentNightAndTime(roomId);

                return TypedResults.Ok(new APIResponse<DayDto>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/latest-deaths",
            (GameService gameService, string roomId) =>
            {
                var state = gameService.GetLatestDeaths(roomId);

                return TypedResults.Ok(new APIResponse<List<PlayerDTO>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/check-win-condition",
            (GameService gameService, string roomId) =>
            {
                var state = gameService.GetWinConditionForRoom(roomId);

                return TypedResults.Ok(new APIResponse<WinCondition>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/summary",
            (GameService gameService, string roomId) =>
            {
                var state = gameService.GetGameSummary(roomId);

                return TypedResults.Ok(new APIResponse<List<GameNightHistoryDTO>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();
    }
}