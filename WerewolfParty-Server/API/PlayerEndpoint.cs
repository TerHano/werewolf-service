using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.API;

public static class PlayerEndpoint
{
    public static void RegisterPlayerEndpoints(this WebApplication app)
    {
        app.MapPost("/api/player/get-id", (HttpContext httpContext, JwtService jwtService) =>
        {
            var playerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var token = playerId != null ? jwtService.RefreshToken( Guid.Parse(playerId)) : jwtService.GenerateToken();
            return TypedResults.Ok(new APIResponse<string>()
            {
                Success = true,
                Data = token
            });
        });
        
        app.MapPost("/api/player/{roomId}/update-player", (string roomId, AddUpdatePlayerDetailsDTO playerDTO, IHubContext<EventsHub, IClientEventsHub> hubContext ,HttpContext httpContext, RoomService roomService)=>
        {
            var playerGuid = Util.GetPlayerGuidFromHttpContext(httpContext);
            var updatedPlayer = roomService.UpdatePlayerDetailsForRoom(roomId, playerGuid, playerDTO);
            string sanitizedRoomId = roomId.ToUpper();
            hubContext.Clients.Group(sanitizedRoomId).PlayerNameUpdated(updatedPlayer);
            return TypedResults.Ok(new APIResponse()
            {
                Success = true
            });
        }).RequireAuthorization();
    }
}