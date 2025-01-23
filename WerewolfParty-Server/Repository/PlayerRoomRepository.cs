using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class PlayerRoomRepository(PlayerRoomDbContext context, ILogger<PlayerRoomRepository> logger)
{
    public PlayerRoomEntity GetPlayerInRoom(string roomId, Guid playerId)
    {
        var player = context.PlayerRooms.FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId,roomId) &&
            playerRoom.PlayerId == playerId);
        if (player == null)
        {
            throw new PlayerNotFoundException("Player not found");
        }

        return player;
    }

    public List<PlayerRoomEntity> GetPlayersInRoom(string roomId)
    {
        return context.PlayerRooms
            .Where(playerRoom => EF.Functions.ILike(playerRoom.RoomId,roomId)).ToList();
    }

    public List<PlayerRoomEntity> GetPlayersInRoomWithoutModerator(string roomId, Guid moderatorId)
    {
        return context.PlayerRooms.Where(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId,roomId) &&
            !playerRoom.PlayerId.Equals(moderatorId)).ToList();
    }

    public PlayerRoomEntity AddPlayerToRoom(Guid playerId, AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var newPlayerRoom = new PlayerRoomEntity
        {
            PlayerId = playerId,
            RoomId = addEditPlayerDetails.RoomId.ToUpper(),
            NickName = addEditPlayerDetails.NickName!,
            AvatarIndex = addEditPlayerDetails.AvatarIndex.GetValueOrDefault(0),
            Status = PlayerStatus.Active,
        };
        var newPlayer = context.PlayerRooms.Add(newPlayerRoom);
        context.SaveChanges();
        return newPlayer.Entity;
    }

    public PlayerRoomEntity UpdatePlayerInRoom(PlayerRoomEntity player)
    {
        var updatedPlayer = context.PlayerRooms.Update(player);
        context.SaveChanges();
        return updatedPlayer.Entity;
    }

    public List<PlayerRoomEntity> UpdateGroupOfPlayersInRoom(List<PlayerRoomEntity> players)
    {
        List<PlayerRoomEntity> updatedGroupOfPlayers = new List<PlayerRoomEntity>();
        foreach (var player in players)
        {
            var updatedPlayer = context.PlayerRooms.Update(player);
            updatedGroupOfPlayers.Add(updatedPlayer.Entity);
        }

        context.SaveChanges();
        return updatedGroupOfPlayers;
    }

    public bool IsPlayerInRoom(Guid playerId, string roomId)
    {
        return context.PlayerRooms.Any(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId,roomId) &&
            playerRoom.PlayerId.Equals(playerId));
    }

    public PlayerRoomEntity UpdatePlayerInRoom(string roomId, Guid playerId, PlayerRoomEntity player)
    {
        var updatedPlayer = context.Update(player);
        context.SaveChanges();
        return updatedPlayer.Entity;
    }

    public void RemovePlayerFromRoom(string roomId, Guid playerId)
    {
        var playerToRemove = context.PlayerRooms.Include((p)=>p.PlayerRole).FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId,roomId) &&
            playerRoom.PlayerId.Equals(playerId));
        if (playerToRemove == null)
        {
            logger.Log(LogLevel.Warning, "Player is not present in room");
        }
        else
        {
            //Remove dependent data
            playerToRemove.PlayerRole = null;
            context.PlayerRooms.Remove(playerToRemove);
            context.SaveChanges();
        }
    }
}