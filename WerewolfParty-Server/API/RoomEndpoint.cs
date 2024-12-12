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

public static class RoomEndpoint
{
    public static void RegisterRoomEndpoints(this WebApplication app)
    {
        app.MapGet("/api/room/all-rooms", (RoomService roomService) => roomService.GetAllRooms())
            .RequireAuthorization();

        app.MapPost("/api/room/create-room", (HttpContext httpContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var newRoomId = roomService.CreateRoom(playerGuid);
            return TypedResults.Ok(new APIResponse<string>()
            {
                Success = true,
                Data = newRoomId
            });
        }).RequireAuthorization();

        app.MapPost("/api/room/end-game", (RoomIdRequest roomIdRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            roomService.UpdateRoomGameState(roomIdRequest.RoomId, GameState.Lobby);
            var sanitizedRoomId = roomIdRequest.RoomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).GameState(GameState.Lobby);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapPost("/api/room/is-player-in-room", (RoomIdRequest roomIdRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var isPlayerInRoom = roomService.isPlayerInRoom(roomIdRequest.RoomId, playerGuid);
            return TypedResults.Ok(new APIResponse<bool>()
            {
                Success = true,
                Data = isPlayerInRoom
            });
        }).RequireAuthorization();

        app.MapPost("/api/room/{roomId}/start-game", (RoomIdRequest roomIdRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            string roomId = roomIdRequest.RoomId.ToUpper();
            var playerGuid = httpContext.User.GetPlayerId();
            var assignedRoles = roomService.ShuffleAndAssignRoles(roomId);
            roomService.UpdateRoomGameState(roomId, GameState.CardsDealt);
            hubContext.Clients.Group(roomId).GameState(GameState.CardsDealt);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapPost("/api/room/kick-player/", (KickPlayerRequest kickPlayerRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            roomService.RemovePlayerFromRoom(kickPlayerRequest.RoomId, kickPlayerRequest.PlayerToKickId);
            var sanitizedRoomId = kickPlayerRequest.RoomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapPost("/api/room/leave-room", (RoomIdRequest roomIdRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var roomId = roomIdRequest.RoomId.ToUpper();
            var playerGuid = httpContext.User.GetPlayerId();
            roomService.RemovePlayerFromRoom(roomId, playerGuid);
            hubContext.Clients.Group(roomId).PlayersInLobbyUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapPost("/api/room/update-moderator", (UpdateModeratorRequest updateModeratorRequest,
            HttpContext httpContext, IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            string roomId = updateModeratorRequest.RoomId.ToUpper();
            var playerGuid = httpContext.User.GetPlayerId();
            roomService.UpdateModeratorForRoom(roomId, updateModeratorRequest.NewModeratorId);
            hubContext.Clients.Group(roomId).ModeratorUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        }).RequireAuthorization();

        app.MapGet("/api/room/{roomId}/get-moderator", (string roomId, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var mod = roomService.GetModeratorForRoom(roomId);

            return TypedResults.Ok(new APIResponse<PlayerDTO>()
            {
                Success = true,
                Data = mod
            });
        }).RequireAuthorization();


        app.MapPost("/api/room/check-room", (RoomService roomService, RoomIdRequest request) =>
        {
            var doesRoomExist = roomService.DoesRoomExist(request.RoomId);
            return TypedResults.Ok(new APIResponse<bool>()
            {
                Success = true,
                Data = doesRoomExist
            });
        });

        app.MapGet("/api/room/{roomId}/players",
            (RoomService roomService, string roomId) =>
            {
                var players = roomService.GetAllPlayersInRoom(roomId, false);
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

        app.MapPost("/api/room/role-settings", (UpdateRoleSettingsRequest updateRoleSettingsRequest,
            RoomService roomService, IValidator<UpdateRoleSettingsRequest> validator) =>
        {
            var result = validator.Validate(updateRoleSettingsRequest);
            if (!result.IsValid)
            {
                return TypedResults.Ok(new APIResponse()
                {
                    Success = false,
                    ErrorMessages = result.Errors.Select(x => x.ErrorMessage)
                });
            }

            roomService.UpdateRoleSettingsForRoom(updateRoleSettingsRequest);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        });

        app.MapGet("/api/room/{roomId}",
            (RoomService roomService, string roomId) =>
            {
                var room = roomService.GetRoom(roomId);
                return TypedResults.Ok(new APIResponse<RoomEntity>()
                {
                    Success = true,
                    Data = room
                });
            }).RequireAuthorization();
    }
}