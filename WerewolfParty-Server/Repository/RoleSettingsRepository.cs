using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class RoleSettingsRepository(WerewolfDbContext context)

{
    public async Task<RoomSettingsEntity> AddRoleSettings(RoomSettingsEntity roomSettingsEntity)
    {
        var roleSettings = await context.RoleSettings.AddAsync(roomSettingsEntity);
        var entity = roleSettings.Entity;
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateRoleSettings(RoomSettingsEntity roomSettingsEntity)
    {
        context.RoleSettings.Update(roomSettingsEntity);
        await context.SaveChangesAsync();
    }

    public async Task<RoomSettingsEntity> GetRoomSettingsByRoomId(string roomId)
    {
        var roomSettings =
            await context.RoleSettings.FirstOrDefaultAsync(r =>
                EF.Functions.ILike(r.RoomId, roomId));
        if (roomSettings is null)
        {
            throw new Exception("Room settings not found");
        }

        return roomSettings;
    }

    public async Task<RoomSettingsEntity> GetRoomSettingsById(int roomSettingsId)
    {
        var roomSettings = await context.RoleSettings.FirstOrDefaultAsync(r => r.Id == roomSettingsId);
        if (roomSettings is null)
        {
            throw new Exception("Room settings not found");
        }

        return roomSettings;
    }
}