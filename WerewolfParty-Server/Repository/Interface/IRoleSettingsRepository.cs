using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Repository.Interface;

public interface IRoleSettingsRepository
{
    public RoomSettingsEntity AddRoleSettings(RoomSettingsEntity roomSettingsEntity);
    public RoomSettingsEntity UpdateRoleSettings(RoomSettingsEntity roomSettingsEntity);

    public RoomSettingsEntity GetRoomSettingsById(int roomSettingsId);
    public RoomSettingsEntity GetRoomSettingsByRoomId(string roomId);
}