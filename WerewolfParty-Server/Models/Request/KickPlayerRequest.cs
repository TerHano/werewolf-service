namespace WerewolfParty_Server.Models.Request;

public class KickPlayerRequest : RoomIdRequest
{
    public Guid PlayerToKickId { get; set; }
}