using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class PlayerRoomRepository(PlayerRoomDbContext playerRoomDbContext, ILogger<PlayerRoomRepository> logger) : IPlayerRoomRepository
{
    public PlayerRoomEntity GetPlayerInRoom(string roomId, Guid playerId)
    {
        var player = playerRoomDbContext.PlayerRooms.FirstOrDefault(playerRoom =>
            playerRoom.RoomId == roomId && playerRoom.PlayerGuid == playerId);
        if (player == null)
        {
            throw new PlayerNotFoundException("Player not found");
        }
        
        return player;
    }

    public List<PlayerRoomEntity> GetPlayersInRoom(string roomId)
    {
        return playerRoomDbContext.PlayerRooms.Where(playerRoom => playerRoom.RoomId == roomId).ToList();
    }

    public PlayerRoomEntity AddPlayerToRoom(string roomId, Guid playerId, AddUpdatePlayerDetailsDTO player)
    {
        var newPlayerRoom = new PlayerRoomEntity
        {
            PlayerGuid = playerId,
            RoomId = roomId,
            Name = player.Name,
            AvatarIndex = player.AvatarIndex,
            Status = PlayerStatus.Active
        };
        var newPlayer = playerRoomDbContext.PlayerRooms.Add(newPlayerRoom);
        playerRoomDbContext.SaveChanges();
        return newPlayer.Entity;
    }
    
    public PlayerRoomEntity UpdatePlayerInRoom(PlayerRoomEntity player)
    {
        var updatedPlayer = playerRoomDbContext.PlayerRooms.Update(player);
        playerRoomDbContext.SaveChanges();
        return updatedPlayer.Entity;
    }
    
    public List<PlayerRoomEntity> UpdateGroupOfPlayersInRoom(List<PlayerRoomEntity> players)
    {
        List<PlayerRoomEntity> updatedGroupOfPlayers = new List<PlayerRoomEntity>();
        foreach (var player in players)
        {
            var updatedPlayer = playerRoomDbContext.PlayerRooms.Update(player);
            updatedGroupOfPlayers.Add(updatedPlayer.Entity);
        }
        playerRoomDbContext.SaveChanges();
        return updatedGroupOfPlayers;
    }

    public bool IsPlayerInRoom(Guid playerId, string roomId)
    {
        return playerRoomDbContext.PlayerRooms.Any(playerRoom => playerRoom.RoomId == roomId && playerRoom.PlayerGuid == playerId);
    }

    public PlayerRoomEntity UpdatePlayerInRoom(string roomId, Guid playerId, AddUpdatePlayerDetailsDTO playerDto)
    {
        var playerToUpdate = GetPlayerInRoom(roomId, playerId);
        if (playerToUpdate == null) throw new NullReferenceException("Player does not exist");
        playerToUpdate.Name = playerDto.Name;
        playerToUpdate.AvatarIndex = playerDto.AvatarIndex;
        var updatedPlayer = playerRoomDbContext.Update(playerToUpdate);
        playerRoomDbContext.SaveChanges();
        return updatedPlayer.Entity;
    }

    public void RemovePlayerFromRoom(string roomId, Guid playerId)
    {
        var playerToRemove =  playerRoomDbContext.PlayerRooms.FirstOrDefault(playerRoom =>
            playerRoom.RoomId == roomId && playerRoom.PlayerGuid == playerId);
        if (playerToRemove == null)
        {
            logger.Log(LogLevel.Warning, "Player is not present in room");
        }
        else
        {
            playerRoomDbContext.PlayerRooms.Remove(playerToRemove);
        }
    }
}