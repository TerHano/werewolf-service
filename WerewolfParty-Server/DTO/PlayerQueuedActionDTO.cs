using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerQueuedActionDTO
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public ActionType Action { get; set; }
    public Guid AffectedPlayerId { get; set; }
}