namespace WerewolfParty_Server.Models.Request;

public class RoomIdRequest : IRoomScopedRequest
{
    public string RoomId { get; set; }
}