using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.DTO;

public class AddEditPlayerDetailsDTO : IRoomScopedRequest
{
    public string RoomId { get; set; }
    public string? NickName { get; set; }
    public int? AvatarIndex { get; set; }
}