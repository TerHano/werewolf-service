using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class RoleSettingsRepository(WerewolfDbContext context)

{
    public async Task<RoomSettingsEntity> AddRoleSettings(RoomSettingsEntity roomSettingsEntity)
    {
        var roleSettings = context.RoleSettings.Add(roomSettingsEntity).Entity;
        await context.SaveChangesAsync();
        return roleSettings;
    }

    public async Task UpdateRoleSettings(RoomSettingsEntity roomSettingsEntity)
    {
        context.RoleSettings.Update(roomSettingsEntity);
        await context.SaveChangesAsync();
    }

    public RoomSettingsEntity GetRoomSettingsByRoomId(string roomId)
    {
        var roomSettings =
            context.RoleSettings.FirstOrDefault(r =>
                EF.Functions.ILike(r.RoomId, roomId));
        if (roomSettings is null)
        {
            throw new Exception("Room settings not found");
        }

        return roomSettings;
    }

    public RoomSettingsEntity GetRoomSettingsById(int roomSettingsId)
    {
        var roomSettings = context.RoleSettings.FirstOrDefault(r => r.Id == roomSettingsId);
        if (roomSettings is null)
        {
            throw new Exception("Room settings not found");
        }

        return roomSettings;
    }
}