using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerQueuedActionDTO
{
    public int Id { get; set; }
    public int PlayerRoleId { get; set; }
    public ActionType Action { get; set; }
    public int AffectedPlayerRoleId { get; set; }
}