using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

/// <summary>
/// The public state of the night call. Every player sees the same thing: which step is running
/// and how long is left. What anybody chose during a step is not in here.
/// </summary>
public class NightStateDto
{
    public bool SelfModerated { get; set; }

    /// <summary>Null when no night call is in progress — day, lobby, or waiting to begin.</summary>
    public NightStep? CurrentStep { get; set; }

    /// <summary>UTC. Null whenever <see cref="CurrentStep"/> is null.</summary>
    public DateTime? StepDeadline { get; set; }

    public int CurrentNight { get; set; }
    public bool IsDay { get; set; }

    /// <summary>
    /// The full running order for this room, so a client can show progress through the night
    /// and, on reconnect, know where it is. Fixed for the whole game.
    /// </summary>
    public List<NightStep> Steps { get; set; } = new();
}
