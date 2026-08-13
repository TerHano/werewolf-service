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
    /// The night call has moved on. Deliberately carries nothing — not the step, not a
    /// deadline, not a position in the order — so the room learns only that something changed
    /// and can refresh. Naming the step here is what would let the table read off who has died,
    /// from how long each step lasts.
    /// </summary>
    Task NightAdvanced();

    /// <summary>
    /// Sent to a single player when the step they act in begins. The in-app twin of the push
    /// notification, and the only night event that is not broadcast to the room — everyone
    /// learns <i>which</i> step is running, but only the people who act in it are told it is
    /// their turn.
    /// </summary>
    Task YourTurn(NightStep step, DateTime deadline);

    /// <summary>
    /// The badge holder gave the current step more time. Sent to the acting players, who are
    /// the only ones with a countdown to correct.
    /// </summary>
    Task StepExtended(NightStep step, DateTime deadline);

    /// <summary>Queued actions have been applied and the room has moved into day.</summary>
    Task NightResolved();
}