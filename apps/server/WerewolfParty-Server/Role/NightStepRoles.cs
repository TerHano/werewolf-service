using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

/// <summary>
/// Which role acts at which step of the night call.
///
/// Shared deliberately: the engine uses it to decide the running order, and authorization uses
/// it to decide whether a caller is allowed to act right now. If those two ever disagreed, a
/// player could submit during someone else's step, so they must read from one table.
/// </summary>
public static class NightStepRoles
{
    private static readonly (NightStep Step, RoleName Role)[] Map =
    [
        (NightStep.WerewolfKill, RoleName.WereWolf),
        (NightStep.DoctorHeal, RoleName.Doctor),
        (NightStep.DetectiveInvestigate, RoleName.Detective),
        (NightStep.WitchAct, RoleName.Witch),
        (NightStep.VigilanteShoot, RoleName.Vigilante)
    ];

    /// <summary>In running order.</summary>
    public static IReadOnlyList<(NightStep Step, RoleName Role)> All => Map;

    /// <summary>
    /// The step this role acts in, or null for roles with no night action (Villager, Drunk,
    /// Cursed) — those can never act at night.
    /// </summary>
    public static NightStep? StepForRole(RoleName role)
    {
        foreach (var entry in Map)
        {
            if (entry.Role == role) return entry.Step;
        }

        return null;
    }
}
