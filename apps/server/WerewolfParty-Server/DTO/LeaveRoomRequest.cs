using System.ComponentModel.DataAnnotations;
using WerewolfParty_Server.Models.Request;

namespace WerewolfParty_Server.DTO;

public class LeaveRoomRequest: RoomIdRequest
{
    [Required]
    public string ConnectionId { get; set; }
}