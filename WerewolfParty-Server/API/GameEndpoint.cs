using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Models.Request;
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
                var currentModerator = roomService.GetModeratorForRoom(roomId);
                if (playerGuid != currentModerator.Id)
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


        app.MapGet("/api/game/{roomId}/{playerGuid:guid}/role-actions",
            (HttpContext httpContext, GameService gameService, string roomId, Guid playerGuid) =>
            {
                var state = gameService.GetActionsForPlayerRole(roomId, playerGuid);
                return TypedResults.Ok(new APIResponse<List<RoleActionDto>>()
                {
                    Success = true,
                    Data = state
                });
            }).RequireAuthorization();

        app.MapGet("/api/game/{roomId}/{playerGuid:guid}/queued-action",
            (HttpContext httpContext, GameService gameService, string roomId, Guid playerGuid) =>
            {
                var state = gameService.GetPlayerQueuedAction(roomId, playerGuid);

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

        app.MapPost("/api/game/queued-action", (PlayerActionRequestDTO playerActionRequestDto, GameService gameService) =>
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
            IHubContext<EventsHub, IClientEventsHub> hubContext,GameService gameService) =>
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
        
        app.MapPost("/api/game/vote-out-player", (PlayerIdAndRoomIdRequestDto request,
            IHubContext<EventsHub, IClientEventsHub> hubContext,GameService gameService) =>
        {
            gameService.LynchChosenPlayer(request.RoomId, request.PlayerId);
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
    }
}