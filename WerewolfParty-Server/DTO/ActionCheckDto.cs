using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DTO;

public class ActionCheckDto
{
    public PlayerRoleEntity CurrentPlayer { get; set; }
    public List<RoomGameActionEntity> ProcessedActions { get; set; }
    public List<RoomGameActionEntity> QueuedActions { get; set; }
    public List<PlayerRoleEntity> ActivePlayers { get; set; }
}