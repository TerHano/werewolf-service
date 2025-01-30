using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.DTO;

public class PlayerActionRequestDTO : RoomIdRequest
{
    public int? PlayerRoleId { get; set; }
    public ActionType Action { get; set; }
    public int AffectedPlayerRoleId { get; set; }
}