using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Repository.Interface;

public interface IPlayerRoomRepository
{
    PlayerRoomEntity? GetPlayerInRoom(string roomId, Guid playerId);
    List<PlayerRoomEntity> GetPlayersInRoom(string roomId);
    PlayerRoomEntity AddPlayerToRoom(string roomId, Guid playerId, AddEditPlayerDetailsDTO player);
    PlayerRoomEntity UpdatePlayerInRoom(string roomId, Guid playerId, AddEditPlayerDetailsDTO playerDto);
    public List<PlayerRoomEntity> UpdateGroupOfPlayersInRoom(List<PlayerRoomEntity> players);
    void RemovePlayerFromRoom(string roomId, Guid playerId);
    bool IsPlayerInRoom(Guid playerId, string roomId);
}