using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

[Table("room_game_action")]
public class RoomGameActionEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("room_id")]
    public required string RoomId { get; set; }
    public RoomEntity Room { get; set; }
    [Column("player_id")]
    public Guid? PlayerId { get; set; }
    [Column("action_type")]
    public required ActionType Action { get; set; }
    [Column("affected_player_id")]
    public required Guid AffectedPlayerId { get; set; }
    [Column("night")]
    public required int Night { get; set; }
    [Column("action_state")]
    public required ActionState State { get; set; }
}