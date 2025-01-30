using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerVoteOutRequestDTO
{
    public int? PlayerRoleId { get; set; }
    public required string RoomId { get; set; }
}