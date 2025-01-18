using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

[Table("player_room")]
public class PlayerRoomEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("player_guid")]
    public required Guid PlayerGuid { get; set; }
    [Column("room_id")]
    public required string RoomId { get; set; }
    public RoomEntity Room { get; set; }

    [Column("nickname")]
    public required string NickName { get; set; }
    [Column("avatar_index")]
    public required int AvatarIndex { get; set; }
    [Column("player_status")]
    public required PlayerStatus Status { get; set; }
    [Column("assigned_role")]
    public RoleName? AssignedRole { get; set; }
    [Column("is_alive")]
    public required bool isAlive { get; set; }
}