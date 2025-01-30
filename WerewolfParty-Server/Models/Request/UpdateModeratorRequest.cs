namespace WerewolfParty_Server.Models.Request;

public class UpdateModeratorRequest : RoomIdRequest
{
    public int NewModeratorPlayerRoomId { get; set; }
}