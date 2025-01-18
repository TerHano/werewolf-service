using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.API;

public static class PlayerEndpoint
{
    public static void RegisterPlayerEndpoints(this WebApplication app)
    {
        app.MapPost("/api/player/get-id", (HttpContext httpContext, JwtService jwtService) =>
        {
            var token = "";
            try
            {
                var playerId = httpContext.User.GetPlayerId();
                token = jwtService.GenerateToken(playerId);
            }
            catch (Exception ex)
            {
                token = jwtService.GenerateToken();
            }

            return TypedResults.Ok(new APIResponse<string>()
            {
                Success = true,
                Data = token
            });
        });

        app.MapPost("/api/player/{roomId}/update-player", (string roomId, AddEditPlayerDetailsDTO playerDTO,
            IHubContext<EventsHub, IClientEventsHub> hubContext, HttpContext httpContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var updatedPlayer = roomService.UpdatePlayerDetailsForRoom(roomId, playerGuid, playerDTO);
            string sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true
            });
        }).RequireAuthorization();

        app.MapGet("/api/player/{roomId}/player", (string roomId,
            HttpContext httpContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var currentPlayer = roomService.GetPlayerInRoom(roomId, playerGuid);
            return TypedResults.Ok(new APIResponse<PlayerDTO>()
            {
                Success = true,
                Data = currentPlayer
            });
        }).RequireAuthorization();
    }
}