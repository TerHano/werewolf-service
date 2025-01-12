using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

public class RoomGameActionEntity
{
    public int Id { get; set; }
    public string RoomId { get; set; }
    public Guid PlayerId { get; set; }
    public ActionType Action { get; set; }
    public Guid AffectedPlayerId { get; set; }
    public int Night { get; set; }
    public ActionState State { get; set; }
}