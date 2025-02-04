using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Exceptions;

namespace WerewolfParty_Server.Repository;

public class PlayerRoleRepository(WerewolfDbContext context, ILogger<PlayerRoomRepository> logger)
{
    public List<PlayerRoleEntity> GetPlayerRolesForRoom(string roomId)
    {
        return context.PlayerRoles
            .Where(playerRole => EF.Functions.ILike(playerRole.RoomId, roomId)).Include(p => p.PlayerRoom).ToList();
    }

    public async Task<PlayerRoleEntity> UpdatePlayerRoleInRoom(PlayerRoleEntity player)
    {
        var updatedPlayer = context.PlayerRoles.Update(player);
        await context.SaveChangesAsync();
        return updatedPlayer.Entity;
    }

    public async Task RemoveAllPlayerRolesForRoom(string roomId)
    {
        context.PlayerRoles.RemoveRange(context.PlayerRoles.Where(playerRole => playerRole.RoomId == roomId));
        await context.SaveChangesAsync();
    }

    public async Task AddPlayerRolesToRoom(List<PlayerRoleEntity> playerRoles)
    {
        context.PlayerRoles.AddRange(playerRoles);
        await context.SaveChangesAsync();
    }

    public PlayerRoleEntity GetPlayerRoleInRoom(string roomId, int playerRoleId)
    {
        var player = context.PlayerRoles.FirstOrDefault(playerRole =>
            EF.Functions.ILike(playerRole.RoomId, roomId) &&
            playerRole.Id == playerRoleId);
        if (player == null)
        {
            throw new PlayerNotFoundException("No role for player found.");
        }

        return player;
    }

    public PlayerRoleEntity GetPlayerRoleInRoomUsingPlayerGuid(string roomId, Guid playerGuid)
    {
        var player = context.PlayerRoles.Include(p => p.PlayerRoom).FirstOrDefault(playerRole =>
            EF.Functions.ILike(playerRole.RoomId, roomId) &&
            playerRole.PlayerRoom.PlayerId == playerGuid);
        if (player == null)
        {
            throw new PlayerNotFoundException("No role for player found.");
        }

        return player;
    }

    public bool DoesPlayerHaveRoleInRoom(string roomId, Guid playerId)
    {
        var player = context.PlayerRoles.FirstOrDefault(playerRoom =>
            EF.Functions.ILike(playerRoom.RoomId, roomId) &&
            playerRoom.PlayerRoom.PlayerId == playerId);
        return player != null;
    }

    public async Task UpdatePlayerIsAliveStatus(List<Guid> playerIds, bool isAlive)
    {
        foreach (var playerId in playerIds)
        {
            var player = context.PlayerRoles.FirstOrDefault((player) => player.PlayerRoom.PlayerId == playerId);
            if (player == null) continue;
            player.IsAlive = isAlive;
            context.Update(player);
        }

        await context.SaveChangesAsync();
    }

    public async Task UpdatePlayerStatusToDead(List<int> playerIds, int night)
    {
        foreach (var playerId in playerIds)
        {
            var player = context.PlayerRoles.FirstOrDefault((player) => player.Id == playerId);
            if (player == null) continue;
            player.IsAlive = false;
            player.NightKilled = night;
            context.Update(player);
        }

        await context.SaveChangesAsync();
    }
}