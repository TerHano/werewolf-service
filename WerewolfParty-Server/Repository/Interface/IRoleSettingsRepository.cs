using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Repository.Interface;

public interface IRoleSettingsRepository
{
    public RoleSettingsEntity AddRoleSettings(RoleSettingsEntity roleSettingsEntity);
    public RoleSettingsEntity UpdateRoleSettings(RoleSettingsEntity roleSettingsEntity);

    public RoleSettingsEntity GetRoomSettingsById(int roomSettingsId);
    public RoleSettingsEntity GetRoomSettingsByRoomId(string roomId);
}