namespace WerewolfParty_Server.DTO;

public class AddEditPlayerDetailsDTO
{
    public string RoomId { get; set; }
    public string? NickName { get; set; }
    public int? AvatarIndex { get; set; }
}