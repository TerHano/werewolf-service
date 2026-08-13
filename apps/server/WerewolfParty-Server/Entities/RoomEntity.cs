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

    /// <summary>
    /// Which stage of the server-run night call is live, or null when no night call is in
    /// progress. Only ever set for self-moderated rooms.
    /// </summary>
    [Column("night_step")] public NightStep? NightStep { get; set; }

    /// <summary>
    /// When the current step ends. The deadline is the only thing that advances the night —
    /// see <see cref="Service.NightEngineService"/> — so this and <see cref="NightStep"/> are
    /// always set and cleared together.
    /// </summary>
    [Column("night_step_deadline")] public DateTime? NightStepDeadline { get; set; }

    /// <summary>
    /// The player role ids that have locked in for the current step. Cleared on every step
    /// transition. When everyone who can act in a step has locked in, the step ends early.
    /// </summary>
    [Column("night_step_locked_in")] public List<int> NightStepLockedIn { get; set; } = new();

    /// <summary>
    /// Whether the moderator badge has already been handed to the first player eliminated.
    /// Set once per game and never again — otherwise the badge would migrate with every death
    /// and nobody would settle into the job.
    /// </summary>
    [Column("moderator_badge_assigned")] public bool ModeratorBadgeAssigned { get; set; }

    public RoomSettingsEntity? RoleSettings { get; set; }
    public List<RoomGameActionEntity>? GameActions { get; set; }
    public List<PlayerRoomEntity>? PlayersInRoom { get; set; }
}