using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

[Table("room")]
public class RoomEntity
{
    [Column("id")] public required string Id { get; set; }
    [Column("current_moderator")] public int? CurrentModeratorId { get; set; }
    public PlayerRoomEntity? CurrentModerator { get; set; }
    [Column("game_state")] public required GameState GameState { get; set; }
    [Column("current_night")] public required int CurrentNight { get; set; }
    [Column("is_day")] public required bool isDay { get; set; }
    [Column("win_condition")] public required WinCondition WinCondition { get; set; }
    [Column("last_modified_date")] public required DateTime LastModifiedDate { get; set; }

    public RoomSettingsEntity? RoleSettings { get; set; }
    public List<RoomGameActionEntity>? GameActions { get; set; }
    public List<PlayerRoomEntity>? PlayersInRoom { get; set; }
}