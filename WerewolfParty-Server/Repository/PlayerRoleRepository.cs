using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Exceptions;

namespace WerewolfParty_Server.Repository;

public class PlayerRoleRepository(WerewolfDbContext context, ILogger<PlayerRoomRepository> logger)
{
    public async Task<List<PlayerRoleEntity>> GetPlayerRolesForRoom(string roomId)
    {
        return await context.PlayerRoles
            .Where(playerRole => EF.Functions.ILike(playerRole.RoomId, roomId)).Include(p => p.PlayerRoom).ToListAsync();
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
        await context.PlayerRoles.AddRangeAsync(playerRoles);
        await context.SaveChangesAsync();
    }

    public async Task<PlayerRoleEntity> GetPlayerRoleInRoom(string roomId, int playerRoleId)
    {
        var player = await context.PlayerRoles.FirstOrDefaultAsync(playerRole =>
            EF.Functions.ILike(playerRole.RoomId, roomId) &&
            playerRole.Id == playerRoleId);
        if (player == null)
        {
            throw new PlayerNotFoundException("No role for player found.");
        }

        return player;
    }

    public async Task<PlayerRoleEntity> GetPlayerRoleInRoomUsingPlayerGuid(string roomId, Guid playerGuid)
    {
        var player = await context.PlayerRoles.Include(p => p.PlayerRoom).FirstOrDefaultAsync(playerRole =>
            EF.Functions.ILike(playerRole.RoomId, roomId) &&
            playerRole.PlayerRoom.PlayerId == playerGuid);
        if (player == null)
        {
            throw new PlayerNotFoundException("No role for player found.");
        }

        return player;
    }

    public async Task<bool>  DoesPlayerHaveRoleInRoom(string roomId, Guid playerId)
    {
        var player = await context.PlayerRoles.FirstOrDefaultAsync(playerRoom =>
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
            var player = await context.PlayerRoles.FirstOrDefaultAsync((player) => player.Id == playerId);
            if (player == null) continue;
            player.IsAlive = false;
            player.NightKilled = night;
            context.Update(player);
        }

        await context.SaveChangesAsync();
    }
}