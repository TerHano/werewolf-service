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

    // public override async Task OnDisconnectedAsync(Exception? exception)
    // {
    //     Console.WriteLine("Client Disconnected");
    //     Console.WriteLine(GetPlayerId());
    //     await base.OnDisconnectedAsync(exception);
    // }

    public async Task<SocketResponse> JoinRoom(AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var roomId = addEditPlayerDetails.RoomId;
        var playerGuid = GetPlayerId();
        if (string.IsNullOrEmpty(roomId)) return new SocketResponse(false, "Room ID is required");
        var doesRoomExist = roomService.DoesRoomExist(roomId);
        if (!doesRoomExist) return new SocketResponse(false, "Room does not exist");
        await roomService.AddPlayerToRoom(playerGuid, addEditPlayerDetails);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToUpper());
        await Clients.OthersInGroup(roomId.ToUpper()).PlayersInLobbyUpdated();
        return new SocketResponse(true);
    }

    private Guid GetPlayerId()
    {
        if (Context.User == null)
        {
            throw new HubException("No player found");
        }

        var playerIdStr = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (playerIdStr == null)
        {
            throw new HubException("No player found");
        }

        return Guid.Parse(playerIdStr);
    }
}