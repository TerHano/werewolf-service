using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerGameActionDTO
{
    public required int Id { get; set; }
    public PlayerRoleDTO Player { get; set; }
    public required ActionType Action { get; set; }
    public required PlayerRoleDTO AffectedPlayer { get; set; }
}