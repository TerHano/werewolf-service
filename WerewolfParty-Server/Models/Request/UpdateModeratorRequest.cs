namespace WerewolfParty_Server.Models.Request;

public class UpdateModeratorRequest : RoomIdRequest
{
    public Guid NewModeratorId { get; set; }
}