using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DTO;

public class ActionCheckDto
{
    public required PlayerRoleEntity CurrentPlayer { get; set; }
    public required List<RoomGameActionEntity> ProcessedActions { get; set; }
    public required List<RoomGameActionEntity> QueuedActions { get; set; }
    public required List<PlayerRoleEntity> ActivePlayers { get; set; }
    public required RoomSettingsEntity Settings { get; set; }
}