namespace WerewolfParty_Server.Models.Request;

public class KickPlayerRequest : RoomIdRequest
{
    public int PlayerRoomIdToKick { get; set; }
}