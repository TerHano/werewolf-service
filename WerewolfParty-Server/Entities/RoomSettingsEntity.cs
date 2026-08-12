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

    /// <summary>
    /// When true the server runs the night itself and everybody — including the room's
    /// moderator — is dealt a role. When false the classic flow applies: a human moderator
    /// calls the night and sits the game out.
    ///
    /// Defaults to true now that the night engine runs. Groups who prefer a human caller can
    /// turn it off in room settings and get the classic flow back unchanged.
    /// </summary>
    [Column("self_moderated")] public bool SelfModerated { get; set; } = true;

    /// <summary>
    /// How long each step of the server-run night call lasts. Every step runs for exactly this
    /// long, including steps nobody can act in — uniform timing is what stops the length of a
    /// step revealing whether its role is still alive.
    /// </summary>
    [Column("night_step_seconds")] public int NightStepSeconds { get; set; } = 45;

    public RoomEntity? Room { get; set; }
}