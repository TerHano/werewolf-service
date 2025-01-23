using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

[Table("player_room")]
public class PlayerRoomEntity
{
    [Key] [Column("id")] public int Id { get; set; }
    
    [Column("player_id")]
    public required Guid PlayerId { get; set; }
    [Column("room_id")]
    public required string RoomId { get; set; }
    public RoomEntity Room { get; set; }

    [Column("nickname")]
    public required string NickName { get; set; }
    [Column("avatar_index")]
    public required int AvatarIndex { get; set; }
    [Column("player_status")]
    public required PlayerStatus Status { get; set; }
    [Column("player_role_id")]
    public PlayerRoleEntity? PlayerRole { get; set; }
}