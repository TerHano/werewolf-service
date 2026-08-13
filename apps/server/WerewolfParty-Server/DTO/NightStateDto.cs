using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

/// <summary>
/// The night as one particular caller may see it.
///
/// The night is opaque by design: everyone learns that a night call is under way, and only the
/// players who act in the running step learn which step it is or when it ends. That is what
/// lets a step finish as soon as its actors have locked in — if the room could see the step, a
/// short one would mean somebody acted and a long one would mean the role is dead.
/// </summary>
public class NightStateDto
{
    public bool SelfModerated { get; set; }

    /// <summary>True while the server is walking the night. Safe for anyone to know.</summary>
    public bool IsNightCallRunning { get; set; }

    /// <summary>
    /// The running step — <b>only</b> when the caller acts in it. Null for everyone else, which
    /// is also how a client knows it is not their turn.
    /// </summary>
    public NightStep? CurrentStep { get; set; }

    /// <summary>UTC. Set only alongside <see cref="CurrentStep"/>.</summary>
    public DateTime? StepDeadline { get; set; }

    /// <summary>Whether the caller has already locked in for this step.</summary>
    public bool HasLockedIn { get; set; }

    public int CurrentNight { get; set; }
    public bool IsDay { get; set; }
}
