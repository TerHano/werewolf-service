using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class PlayerRoleRepository(PlayerRoleDbContext context, ILogger<PlayerRoomRepository> logger)
{
    public List<PlayerRoleEntity> GetPlayerRolesForRoom(string roomId)
    {
        return context.PlayerRoles
            .Where(playerRole => EF.Functions.ILike(playerRole.RoomId,roomId)).Include(p=>p.PlayerRoom).ToList();
    }
    
    public PlayerRoleEntity UpdatePlayerRoleInRoom(PlayerRoleEntity player)
    {
        var updatedPlayer = context.PlayerRoles.Update(player);
        context.SaveChanges();
        return updatedPlayer.Entity;
    }

    public void RemoveAllPlayerRolesForRoom(string roomId)
    {
        context.PlayerRoles.RemoveRange(context.PlayerRoles.Where(playerRole => playerRole.RoomId == roomId));
        context.SaveChanges();
    }
    
    public void AddPlayerRolesToRoom(List<PlayerRoleEntity> playerRoles)
    {
        context.PlayerRoles.AddRange(playerRoles);
        context.SaveChanges();
    }
    
    public PlayerRoleEntity GetPlayerRoleInRoom(string roomId, Guid playerId)
    {
        var player = context.PlayerRoles.Include(p=>p.PlayerRoom).FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId,roomId) &&
            playerRoom.PlayerRoom.PlayerId == playerId);
        if (player == null)
        {
            throw new PlayerNotFoundException("No role for player found.");
        }
        return player;
    }
    
    public bool DoesPlayerHaveRoleInRoom(string roomId, Guid playerId)
    {
        var player = context.PlayerRoles.FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId,roomId) &&
            playerRoom.PlayerRoom.PlayerId == playerId);
        return player != null;
    }
    
    public void UpdatePlayerIsAliveStatus(List<Guid> playerIds, bool isAlive)
    {
        foreach (var playerId in playerIds)
        {
            var player = context.PlayerRoles.FirstOrDefault((player) => player.PlayerRoom.PlayerId == playerId);
            if (player == null) continue;
            player.IsAlive = isAlive;
            context.Update(player);
        }

        context.SaveChanges();
    }
    public void UpdatePlayerStatusToDead(List<Guid> playerIds, int night)
    {
        foreach (var playerId in playerIds)
        {
            var player = context.PlayerRoles.FirstOrDefault((player) => player.PlayerRoom.PlayerId == playerId);
            if (player == null) continue;
            player.IsAlive = false;
            player.NightKilled = night;
            context.Update(player);
        }
        context.SaveChanges();
    }
  
}