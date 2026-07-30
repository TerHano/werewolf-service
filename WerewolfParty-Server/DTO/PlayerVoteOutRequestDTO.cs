using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.DTO;

public class PlayerVoteOutRequestDTO : IRoomScopedRequest
{
    public int? PlayerRoleId { get; set; }
    public required string RoomId { get; set; }
}