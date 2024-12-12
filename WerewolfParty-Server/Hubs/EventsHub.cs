using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Models;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.Hubs;

public class EventsHub(RoomService roomService) : Hub<IClientEventsHub>
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("Client Connected");
        await base.OnConnectedAsync();
    }

    public async Task<SocketResponse> JoinRoom(string roomId, AddUpdatePlayerDetailsDTO player)
    {
        var playerGuid = GetPlayerId();
        if (string.IsNullOrEmpty(roomId)) return new SocketResponse(false, "Room ID is required");
        var sanitizedRoomId = roomId.ToUpper();
        var doesRoomExist = roomService.DoesRoomExist(sanitizedRoomId);
        if (!doesRoomExist) return new SocketResponse(false, "Room does not exist");
        await Groups.AddToGroupAsync(Context.ConnectionId, sanitizedRoomId);
        roomService.AddPlayerToRoom(sanitizedRoomId, playerGuid, player);
        await Clients.Group(sanitizedRoomId).PlayersInLobbyUpdated();
        return new SocketResponse(true);
    }

    private Guid GetPlayerId()
    {
        if (Context.User == null)
        {
            throw new HubException("No player found");
        }

        var playerIdStr = Context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
        if (playerIdStr == null)
        {
            throw new HubException("No player found");
        }

        return Guid.Parse(playerIdStr);
    }
}