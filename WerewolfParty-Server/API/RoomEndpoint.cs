using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.API;

public static class RoomEndpoint
{
    public static void RegisterRoomEndpoints(this WebApplication app)
    {
        app.MapGet("/api/room/all-rooms", (HttpContext httpContext, RoomService roomService) => roomService.GetAllRooms())
            .RequireAuthorization();

        app.MapPost("/api/room/create-room", (HttpContext httpContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            var newRoomId = roomService.CreateRoom(playerGuid);
            return TypedResults.Ok(new APIResponse<string>()
            {
                Success = true,
                Data = newRoomId
            });
        }).RequireAuthorization();
        
        app.MapPost("/api/room/{roomId}/end-game", (string roomId,HttpContext httpContext, IHubContext<EventsHub,IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            roomService.UpdateRoomGameState(roomId, GameState.Lobby);
            var sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).GameState(GameState.Lobby);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();
        
        app.MapPost("/api/room/{roomId}/is-player-in-room", (string roomId,HttpContext httpContext, IHubContext<EventsHub,IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            roomService.GetPlayerInRoom(roomId, playerGuid);
            var sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).GameState(GameState.Lobby);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();
        
        app.MapPost("/api/room/{roomId}/start-game", (string roomId,HttpContext httpContext, IHubContext<EventsHub,IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            var assignedRoles = roomService.ShuffleAndAssignRoles(roomId);
            roomService.UpdateRoomGameState(roomId, GameState.CardsDealt);
            var sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).GameState(GameState.CardsDealt);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();
        
        app.MapPost("/api/room/{roomId}/kick-player/{playerToKickId:guid}", (string roomId,Guid playerToKickId,HttpContext httpContext, IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            roomService.RemovePlayerFromRoom(roomId, playerToKickId);
            var sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();
        
        app.MapPost("/api/room/{roomId}/leave-room", (string roomId,HttpContext httpContext, IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            roomService.RemovePlayerFromRoom(roomId, playerGuid);
            var sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();
        
        app.MapPost("/api/room/{roomId}/update-moderator/{playerToMakeMod:guid}", (string roomId, Guid playerToMakeMod, HttpContext httpContext, IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            roomService.UpdateModeratorForRoom(roomId, playerToMakeMod);
            var sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).ModeratorUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();
        
        app.MapGet("/api/room/{roomId}/get-moderator", (string roomId, HttpContext httpContext, IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            var mod = roomService.GetModeratorForRoom(roomId);
           
            return TypedResults.Ok(new APIResponse<PlayerDTO>()
            {
                Success = true,
                Data = mod
            });
        }).RequireAuthorization();
        
        
        app.MapPost("/api/room/{roomId}/check-room", (RoomService roomService,  string roomId) =>
        {
            var doesRoomExist = roomService.DoesRoomExist(roomId);
            return TypedResults.Ok(new APIResponse<bool>()
            {
                Success = true,
                Data = doesRoomExist
            });
        });

        app.MapGet("/api/room/{roomId}/players",
            (RoomService roomService, string roomId) =>
            {
                var players= roomService.GetAllPlayersInRoom(roomId, false);
                return TypedResults.Ok(new APIResponse<List<PlayerDTO>>()
                {
                    Success = true,
                    Data = players
                });
            }).RequireAuthorization();
        
        app.MapGet("/api/room/{roomId}/role-settings", (RoomService roomService, string roomId) =>
        {
            var roleSettings = roomService.GetRoleSettingsForRoom(roomId);
            return TypedResults.Ok(new APIResponse<RoleSettingsEntity>()
            {
                Success = true,
                Data = roleSettings
            });
        });
        
        app.MapPost("/api/room/{roomId}/role-settings", (string roomId, RoomService roomService, RoleSettingsEntity roleSettingsEntity, IValidator<RoleSettingsEntity> validator) =>
        {
            var result = validator.Validate(roleSettingsEntity);
            if (!result.IsValid)
            {
                return TypedResults.Ok(new APIResponse()
                {
                    Success = false,
                    ErrorMessages = result.Errors.Select(x => x.ErrorMessage)
                });
            }
            roomService.UpdateRoleSettingsForRoom(roomId, roleSettingsEntity);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        });
    }
}