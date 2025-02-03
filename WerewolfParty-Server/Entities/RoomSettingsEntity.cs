using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

[Table("role_settings")]
public class RoomSettingsEntity
{
    [Key] [Column("id")] public int Id { get; set; }
    [Column("room_id")] [StringLength(10)] public required string RoomId { get; init; }
    [Column("number_of_werewolves")] public required int NumberOfWerewolves { get; set; }
    [Column("selected_roles")] public List<RoleName> SelectedRoles { get; set; } = new();
    [Column("show_game_summary")] public bool ShowGameSummary { get; set; }
    [Column("allow_multiple_self_heals")] public bool AllowMultipleSelfHeals { get; set; }

    public RoomEntity? Room { get; set; }
}