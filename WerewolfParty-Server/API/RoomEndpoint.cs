using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
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
            .WithName("GetAllRooms")
            .WithTags("Room")
            .WithSummary("Get all rooms.")
            .WithDescription("Returns a list of all available rooms.")
            .RequireAuthorization();

        app.MapPost("/api/room/create-room", async (HttpContext httpContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var newRoomId = await roomService.CreateRoom();
            return TypedResults.Ok(new APIResponse<string>()
            {
                Success = true,
                Data = newRoomId
            });
        })
        .WithName("CreateRoom")
        .WithTags("Room")
        .WithSummary("Create a new room.")
        .WithDescription("Creates a new game room and returns its unique identifier.")
        .RequireAuthorization();

        app.MapPost("/api/room/end-game", async (RoomIdRequest roomIdRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            await roomService.UpdateRoomGameState(roomIdRequest.RoomId, GameState.Lobby);
            await hubContext.Clients.Group(roomIdRequest.RoomId.ToUpper()).GameState(GameState.Lobby);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("EndGame")
        .WithTags("Room")
        .WithSummary("End the current game in a room.")
        .WithDescription("Ends the current game in the specified room and returns to lobby state.")
        .RequireAuthorization();

        app.MapGet("/api/room/{roomId}/is-player-in-room",async (string roomId, HttpContext httpContext,
            RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var isPlayerInRoom = await roomService.isPlayerInRoom(roomId, playerGuid);
            return TypedResults.Ok(new APIResponse<bool>()
            {
                Success = true,
                Data = isPlayerInRoom
            });
        })
        .WithName("IsPlayerInRoom")
        .WithTags("Room")
        .WithSummary("Check if player is in room.")
        .WithDescription("Verifies if the current player is a member of the specified room.")
        .RequireAuthorization();

        app.MapPost("/api/room/start-game", async (RoomIdRequest roomIdRequest,
                HttpContext httpContext,
                IHubContext<EventsHub, IClientEventsHub> hubContext,
                GameService gameService) =>
            {
                var roomId = roomIdRequest.RoomId.ToUpper();
                var gameState = await gameService.GetGameState(roomId);
                try
                {
                    await gameService.StartGame(roomId);
                    if (gameState == GameState.CardsDealt)
                    {
                        await hubContext.Clients.Group(roomId).GameRestart();
                    }
                    else
                    {
                        await hubContext.Clients.Group(roomId).GameState(GameState.CardsDealt);
                    }
                }
                catch (NotEnoughPlayersException e)
                {
                    return TypedResults.Ok(new APIResponse()
                    {
                        Success = false,
                        ErrorMessages = new List<string> { e.Message }
                    });
                }
                return TypedResults.Ok(new APIResponse()
                {
                    Success = true,
                });
            })
            .WithName("StartGame")
            .WithTags("Room")
            .WithSummary("Start a game in a room.")
            .WithDescription("Initiates a new game in the specified room, dealing cards to all players.")
            .RequireAuthorization();

        app.MapPost("/api/room/kick-player/", async(KickPlayerRequest kickPlayerRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            await roomService.RemovePlayerFromRoom(kickPlayerRequest.RoomId, kickPlayerRequest.PlayerRoomIdToKick);
            var sanitizedRoomId = kickPlayerRequest.RoomId.ToUpper();
            //hubContext.Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
            await hubContext.Clients.Group(sanitizedRoomId).PlayerKicked(kickPlayerRequest.PlayerRoomIdToKick);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("KickPlayer")
        .WithTags("Room")
        .WithSummary("Kick a player from the room.")
        .WithDescription("Removes a player from the specified room and notifies all room participants.")
        .RequireAuthorization();

        app.MapPost("/api/room/leave-room", async (RoomIdRequest roomIdRequest, HttpContext httpContext,
            IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            var roomId = roomIdRequest.RoomId.ToUpper();
            var oldModerator = roomService.GetModeratorForRoom(roomId);
            var playerGuid = httpContext.User.GetPlayerId();
            var player = roomService.GetPlayerInRoomUsingGuid(roomId, playerGuid);
            await roomService.RemovePlayerFromRoom(roomId, player.Id);
            await hubContext.Clients.Group(roomId).PlayersInLobbyUpdated();
            //Emit moderator change incase mod is replaced
            var newModerator = await roomService.GetModeratorForRoom(roomId);
            if (newModerator != null && oldModerator?.Id != newModerator?.Id)
            {
                await hubContext.Clients.Group(roomId).ModeratorUpdated(newModerator);
            }

            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("LeaveRoom")
        .WithTags("Room")
        .WithSummary("Leave the current room.")
        .WithDescription("Removes the current player from the specified room and notifies other players.")
        .RequireAuthorization();

        app.MapPost("/api/room/update-moderator", async (UpdateModeratorRequest updateModeratorRequest,
            HttpContext httpContext, IHubContext<EventsHub, IClientEventsHub> hubContext, RoomService roomService) =>
        {
            if (updateModeratorRequest.NewModeratorPlayerRoomId == 0)
            {
                throw new Exception("New Moderator Id is required");
            }

            string roomId = updateModeratorRequest.RoomId.ToUpper();
            var playerGuid = httpContext.User.GetPlayerId();
            var newMod = await roomService.UpdateModeratorForRoom(roomId, updateModeratorRequest.NewModeratorPlayerRoomId);
            await hubContext.Clients.Group(roomId).ModeratorUpdated(newMod);
            await hubContext.Clients.Group(roomId).PlayersInLobbyUpdated();

            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("UpdateModerator")
        .WithTags("Room")
        .WithSummary("Update the room moderator.")
        .WithDescription("Assigns a new moderator to the specified room and notifies all participants.")
        .RequireAuthorization();

        app.MapGet("/api/room/{roomId}/get-moderator", async (string roomId, RoomService roomService) =>
        {
            var mod = await roomService.GetModeratorForRoom(roomId);

            return TypedResults.Ok(new APIResponse<PlayerDTO?>()
            {
                Success = true,
                Data = mod
            });
        })
        .WithName("GetModerator")
        .WithTags("Room")
        .WithSummary("Get the room moderator.")
        .WithDescription("Returns the details of the current moderator for the specified room.")
        .RequireAuthorization();


        app.MapPost("/api/room/check-room", async (RoomIdRequest request, RoomService roomService) =>
        {
            var doesRoomExist =await  roomService.DoesRoomExist(request.RoomId);
            return TypedResults.Ok(new APIResponse<bool>()
            {
                Success = true,
                Data = doesRoomExist
            });
        })
        .WithName("CheckRoomExists")
        .WithTags("Room")
        .WithSummary("Check if a room exists.")
        .WithDescription("Verifies if a room with the specified ID exists.")
        .RequireAuthorization();

        app.MapGet("/api/room/{roomId}/players",
           async (RoomService roomService, HttpContext httpContext, string roomId) =>
            {
                var playerGuid = httpContext.User.GetPlayerId();
                var players = await roomService.GetAllPlayersInRoom(roomId, playerGuid, false);
                return TypedResults.Ok(new APIResponse<List<PlayerDTO>>()
                {
                    Success = true,
                    Data = players
                });
            })
        .WithName("GetPlayersInRoom")
        .WithTags("Room")
        .WithSummary("Get all players in a room.")
        .WithDescription("Returns a list of all players currently in the specified room.")
        .RequireAuthorization();

        app.MapGet("/api/room/{roomId}/role-settings", async (RoomService roomService, string roomId) =>
        {
            var roleSettings = await roomService.GetRoleSettingsForRoom(roomId);
            return TypedResults.Ok(new APIResponse<RoomSettingsDto>()
            {
                Success = true,
                Data = roleSettings
            });
        })
        .WithName("GetRoleSettings")
        .WithTags("Room")
        .WithSummary("Get role settings for a room.")
        .WithDescription("Returns the current role settings configuration for the specified room.")
        .RequireAuthorization();

        app.MapPost("/api/room/role-settings", async (UpdateRoleSettingsRequest updateRoleSettingsRequest,
            IHubContext<EventsHub, IClientEventsHub> hubContext,
            RoomService roomService, IValidator<UpdateRoleSettingsRequest> validator) =>
        {
            var result = await validator.ValidateAsync(updateRoleSettingsRequest);
            if (!result.IsValid)
            {
                return TypedResults.Ok(new APIResponse()
                {
                    Success = false,
                    ErrorMessages = result.Errors.Select(x => x.ErrorMessage)
                });
            }

            await roomService.UpdateRoleSettingsForRoom(updateRoleSettingsRequest);
            var sanitizedRoomId = updateRoleSettingsRequest.RoomId.ToUpper();
            await hubContext.Clients.Group(sanitizedRoomId).RoomRoleSettingsUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true,
            });
        })
        .WithName("UpdateRoleSettings")
        .WithTags("Room")
        .WithSummary("Update role settings for a room.")
        .WithDescription("Modifies the role settings configuration for the specified room and notifies all participants.")
        .RequireAuthorization();

        app.MapGet("/api/room/{roomId}",
          async  (RoomService roomService, string roomId) =>
            {
                var room = await roomService.GetRoom(roomId);
                return TypedResults.Ok(new APIResponse<RoomEntity>()
                {
                    Success = true,
                    Data = room
                });
            })
        .WithName("GetRoom")
        .WithTags("Room")
        .WithSummary("Get room details.")
        .WithDescription("Returns detailed information about the specified room.")
        .RequireAuthorization();

        app.MapGet("/api/room/{roomId}/game-state",async (GameService gameService, string roomId) =>
        {
            var state = await gameService.GetGameState(roomId);
            return TypedResults.Ok(new APIResponse<GameState>()
            {
                Success = true,
                Data = state
            });
        })
        .WithName("GetGameState")
        .WithTags("Room")
        .WithSummary("Get the current game state for a room.")
        .WithDescription("Returns the current game state (Lobby, CardsDealt, etc.) for the specified room.")
        .RequireAuthorization();
    }
}
