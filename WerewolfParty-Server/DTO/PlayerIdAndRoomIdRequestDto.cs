using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.DTO;

public class PlayerIdAndRoomIdRequestDto : RoomIdRequest
{
    public Guid? PlayerId { get; set; }
}