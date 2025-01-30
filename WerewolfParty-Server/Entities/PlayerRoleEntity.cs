using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

[Table("player_role")]
public class PlayerRoleEntity
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("room_id")] [MaxLength(10)] public required string RoomId { get; init; }
    [Column("game_role")] public required RoleName Role { get; set; }
    [Column("is_alive")] public required bool IsAlive { get; set; }
    [Column("night_killed")] public int NightKilled { get; set; }
    
    [Column("player_room_id")]
    public required int PlayerRoomId { get; set; }
    public PlayerRoomEntity PlayerRoom { get; set; }
}