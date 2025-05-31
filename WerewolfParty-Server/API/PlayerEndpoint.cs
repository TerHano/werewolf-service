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
            string token;
            try
            {
                var playerId = httpContext.User.GetPlayerId();
                token = jwtService.GenerateToken(playerId);
            }
            catch (Exception)
            {
                token = jwtService.GenerateToken();
            }

            return TypedResults.Ok(new APIResponse<string>()
            {
                Success = true,
                Data = token
            });
        })
        .WithName("GetPlayerIdToken")
        .WithTags("Player")
        .WithSummary("Get player ID token.")
        .WithDescription("Generates and returns a JWT token containing the player's ID. Creates a new ID if none exists.");

        app.MapPost("/api/player/update-player", async (AddEditPlayerDetailsDTO addEditPlayerDetails,
            IHubContext<EventsHub, IClientEventsHub> hubContext, HttpContext httpContext, RoomService roomService) =>
        {
            var roomId = addEditPlayerDetails.RoomId;
            var playerGuid = httpContext.User.GetPlayerId();
            var player = await roomService.GetPlayerInRoomUsingGuid(roomId, playerGuid);
            await roomService.UpdatePlayerDetailsForRoom(player.Id, addEditPlayerDetails);
            string sanitizedRoomId = roomId.ToUpper();
            await hubContext.Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
            return TypedResults.Ok(new APIResponse()
            {
                Success = true
            });
        })
        .WithName("UpdatePlayerDetails")
        .WithTags("Player")
        .WithSummary("Update player details.")
        .WithDescription("Updates the current player's details in a room and notifies other players about the change.")
        .RequireAuthorization();

        app.MapGet("/api/player/{roomId}/player", async (string roomId,
            HttpContext httpContext, RoomService roomService) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var currentPlayer = await roomService.GetPlayerInRoomUsingGuid(roomId, playerGuid);
            return TypedResults.Ok(new APIResponse<PlayerDTO>()
            {
                Success = true,
                Data = currentPlayer
            });
        })
        .WithName("GetCurrentPlayerInRoom")
        .WithTags("Player")
        .WithSummary("Get current player in room.")
        .WithDescription("Returns the current player's details for the specified room.")
        .RequireAuthorization();
    }
}
