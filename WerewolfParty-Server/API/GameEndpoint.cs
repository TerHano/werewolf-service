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
            }).RequireAuthorization();

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
            }).RequireAuthorization();


        app.MapGet("/api/game/{roomId}/{playerRoleId}/role-actions",
            async (GameService gameService, string roomId, int playerRoleId) =>
            {
                var state = await gameService.GetActionsForPlayerRole(roomId, playerRoleId);
                return TypedResults.Ok(new APIResponse<List<RoleActionDto>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/{playerRoleId}/queued-action",
            async (GameService gameService, string roomId, int playerRoleId) =>
            {
                var state = await gameService.GetPlayerQueuedAction(roomId, playerRoleId);

                return TypedResults.Ok(new APIResponse<PlayerQueuedActionDTO>()
                {
                    Success = true,
                    Data = state!
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/all-queued-actions",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetAllQueuedActionsForRoom(roomId);

                return TypedResults.Ok(new APIResponse<List<PlayerQueuedActionDTO>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapPost("/api/game/queued-action",
           async (PlayerActionRequestDTO playerActionRequestDto, GameService gameService) =>
            {
                await gameService.QueueActionForPlayer(playerActionRequestDto);
                return TypedResults.Ok(new APIResponse()
                {
                    Success = true,
                });
            }).RequireAuthorization();

        app.MapDelete("/api/game/queued-action/{actionId}", async (int actionId, GameService gameService) =>
        {
            await gameService.DequeueActionForPlayer(actionId);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

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
        }).RequireAuthorization();

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
        }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/day-time",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetCurrentNightAndTime(roomId);

                return TypedResults.Ok(new APIResponse<DayDto>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/latest-deaths",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetLatestDeaths(roomId);

                return TypedResults.Ok(new APIResponse<List<PlayerDTO>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/check-win-condition",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetWinConditionForRoom(roomId);

                return TypedResults.Ok(new APIResponse<WinCondition>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/summary",
            async (GameService gameService, string roomId) =>
            {
                var state = await gameService.GetGameSummary(roomId);

                return TypedResults.Ok(new APIResponse<List<GameNightHistoryDTO>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();
    }
}