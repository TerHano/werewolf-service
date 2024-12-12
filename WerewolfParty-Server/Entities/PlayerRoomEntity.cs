using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

public class PlayerRoomEntity
{
    public int Id { get; set; }
    public Guid PlayerGuid { get; set; }
    public string RoomId { get; set; }
    public RoomEntity Room { get; set; }

    public string NickName { get; set; }
    public int AvatarIndex { get; set; }
    public PlayerStatus Status { get; set; }
    public RoleName? AssignedRole { get; set; }
}