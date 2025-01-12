using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class RoleSettingsRepository(RoleSettingsDbContext context) : IRoleSettingsRepository

{
    public RoleSettingsEntity AddRoleSettings(RoleSettingsEntity roleSettingsEntity)
    {
        var roleSettings = context.RoleSettings.Add(roleSettingsEntity).Entity;
        context.SaveChanges();
        return roleSettings;
    }

    public RoleSettingsEntity UpdateRoleSettings(RoleSettingsEntity roleSettingsEntity)
    {
        var roleSettings = context.RoleSettings.Update(roleSettingsEntity).Entity;
        context.SaveChanges();
        return roleSettings;
    }

    public RoleSettingsEntity GetRoomSettingsByRoomId(string roomId)
    {
        var roomSettings = context.RoleSettings.FirstOrDefault(r => r.RoomId.Equals(roomId, StringComparison.CurrentCultureIgnoreCase));
        if (roomSettings is null)
        {
            throw new Exception("Room settings not found");
        }

        return roomSettings;
    }

    public RoleSettingsEntity GetRoomSettingsById(int roomSettingsId)
    {
        var roomSettings = context.RoleSettings.FirstOrDefault(r => r.Id == roomSettingsId);
        if (roomSettings is null)
        {
            throw new Exception("Room settings not found");
        }

        return roomSettings;
    }
}