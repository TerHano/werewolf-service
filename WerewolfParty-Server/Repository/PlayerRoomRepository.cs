using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;

namespace WerewolfParty_Server.Repository;

public class PlayerRoomRepository(WerewolfDbContext context, ILogger<PlayerRoomRepository> logger)
{
    public PlayerRoomEntity GetPlayerInRoomUsingPlayerGuid(string roomId, Guid playerId)
    {
        var player = context.PlayerRooms.FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId) &&
            playerRoom.PlayerId == playerId);
        if (player == null)
        {
            throw new PlayerNotFoundException("Player not found");
        }

        return player;
    }

    public PlayerRoomEntity GetPlayerInRoom(string roomId, int playerRoomId)
    {
        var player = context.PlayerRooms.FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId) &&
            playerRoom.Id == playerRoomId);
        if (player == null)
        {
            throw new PlayerNotFoundException("Player not found");
        }

        return player;
    }

    public int GetPlayerCountForRoom(string roomId)
    {
        var count = context.PlayerRooms.Count(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId));
        return count;
    }


    public List<PlayerRoomEntity> GetPlayersInRoom(string roomId)
    {
        return context.PlayerRooms
            .Where(playerRoom => EF.Functions.ILike(playerRoom.RoomId, roomId)).ToList();
    }

    public List<PlayerRoomEntity> GetPlayersInRoomWithoutModerator(string roomId, int? moderatorId)
    {
        return context.PlayerRooms.Where(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId) &&
            !playerRoom.Id.Equals(moderatorId)).ToList();
    }

    public async Task<PlayerRoomEntity> AddPlayerToRoom(Guid playerId, AddEditPlayerDetailsDTO addEditPlayerDetails)
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
        await context.SaveChangesAsync();
        return newPlayer.Entity;
    }

    public async Task<PlayerRoomEntity> UpdatePlayerInRoom(PlayerRoomEntity player)
    {
        var updatedPlayer = context.PlayerRooms.Update(player);
        await context.SaveChangesAsync();
        return updatedPlayer.Entity;
    }

    public async Task<List<PlayerRoomEntity>> UpdateGroupOfPlayersInRoom(List<PlayerRoomEntity> players)
    {
        List<PlayerRoomEntity> updatedGroupOfPlayers = new List<PlayerRoomEntity>();
        foreach (var player in players)
        {
            var updatedPlayer = context.PlayerRooms.Update(player);
            updatedGroupOfPlayers.Add(updatedPlayer.Entity);
        }

        await context.SaveChangesAsync();
        return updatedGroupOfPlayers;
    }

    public bool IsPlayerInRoom(Guid playerId, string roomId)
    {
        return context.PlayerRooms.Any(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId) &&
            playerRoom.PlayerId.Equals(playerId));
    }

    public async Task<PlayerRoomEntity> UpdatePlayerInRoom(string roomId, Guid playerId, PlayerRoomEntity player)
    {
        var updatedPlayer = context.Update(player);
        await context.SaveChangesAsync();
        return updatedPlayer.Entity;
    }

    public async Task RemovePlayerFromRoom(string roomId, int playerRoomId)
    {
        var playerToRemove = context.PlayerRooms.Include((p) => p.PlayerRole).FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId) &&
            playerRoom.Id.Equals(playerRoomId));
        if (playerToRemove == null)
        {
            logger.Log(LogLevel.Warning, "Player is not present in room");
        }
        else
        {
            //Remove dependent data
            playerToRemove.PlayerRole = null;
            context.PlayerRooms.Remove(playerToRemove);
            await context.SaveChangesAsync();
        }
    }
}