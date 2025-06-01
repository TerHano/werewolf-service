using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.DTO;

public class InvestigatePlayerRequest: RoomIdRequest
{
    public int PlayerRoleId { get; set; }
    public InvestigationType InvestigationType { get; set; }
}