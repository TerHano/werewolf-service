using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Repository.Interface;

public interface IPlayerRoomRepository
{
    PlayerRoomEntity? GetPlayerInRoom(string roomId, Guid playerId);
    List<PlayerRoomEntity> GetPlayersInRoom(string roomId);
    PlayerRoomEntity AddPlayerToRoom(string roomId, Guid playerId, AddUpdatePlayerDetailsDTO player);
    PlayerRoomEntity UpdatePlayerInRoom(string roomId, Guid playerId, AddUpdatePlayerDetailsDTO playerDto);
    public List<PlayerRoomEntity> UpdateGroupOfPlayersInRoom(List<PlayerRoomEntity> players);
    void RemovePlayerFromRoom(string roomId, Guid playerId);
    bool IsPlayerInRoom(Guid playerId, string roomId);
}