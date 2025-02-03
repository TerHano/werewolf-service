using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class RoleSettingsRepository(WerewolfDbContext context)

{
    public RoomSettingsEntity AddRoleSettings(RoomSettingsEntity roomSettingsEntity)
    {
        var roleSettings = context.RoleSettings.Add(roomSettingsEntity).Entity;
        context.SaveChanges();
        return roleSettings;
    }

    public void UpdateRoleSettings(RoomSettingsEntity roomSettingsEntity)
    {
        context.RoleSettings.Update(roomSettingsEntity);
        context.SaveChanges();
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