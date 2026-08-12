using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Models;

namespace WerewolfParty_Server.Hubs;

public interface IClientEventsHub
{
    Task GameRestart();
    Task WinConditionMet();
    Task DayTimeUpdated();
    Task GameState(GameState gameState);
    Task PlayersInLobbyUpdated();
    Task ModeratorUpdated(PlayerDTO newModerator);
    Task RoomRoleSettingsUpdated();

    Task PlayerKicked(int kickedPlayerId);

    /// <summary>The server-run night call has begun.</summary>
    Task NightStarted(int night);

    /// <summary>
    /// The night call has moved on to <paramref name="step"/>, ending at
    /// <paramref name="deadline"/> (UTC). Sent to the whole room: which step is running, and how
    /// long it lasts, is public information at any table — what was chosen during it is not.
    /// </summary>
    Task NightStepChanged(NightStep step, DateTime deadline);

    /// <summary>
    /// Sent to a single player when the step they act in begins. The in-app twin of the push
    /// notification, and the only night event that is not broadcast to the room — everyone
    /// learns <i>which</i> step is running, but only the people who act in it are told it is
    /// their turn.
    /// </summary>
    Task YourTurn(NightStep step);

    /// <summary>
    /// The badge holder gave the current step more time. Broadcast so every countdown agrees.
    /// </summary>
    Task StepExtended(NightStep step, DateTime deadline);

    /// <summary>Queued actions have been applied and the room has moved into day.</summary>
    Task NightResolved();
}