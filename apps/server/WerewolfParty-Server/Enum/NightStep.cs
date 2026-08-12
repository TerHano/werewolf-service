namespace WerewolfParty_Server.Enum;

/// <summary>
/// One stage of the server-run night call, in the order the moderator's stepper has always
/// used. A room's current step is stored on <see cref="Entities.RoomEntity.NightStep"/>, and is
/// null whenever no night call is in progress (day, lobby, or waiting for the night to begin).
///
/// The numeric order is the running order — <see cref="Service.NightEngineService"/> walks the
/// values upwards — so new roles must be inserted at the position they should be called, not
/// appended.
/// </summary>
public enum NightStep
{
    WerewolfKill = 0,
    DoctorHeal = 1,
    DetectiveInvestigate = 2,
    WitchAct = 3,
    VigilanteShoot = 4,

    /// <summary>
    /// Every step has run and the queued actions are being applied. Transient: the engine moves
    /// the room out of this and into day within the same operation.
    /// </summary>
    Resolving = 5
}
