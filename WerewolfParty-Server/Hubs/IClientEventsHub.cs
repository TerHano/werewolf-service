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
    Task ModeratorUpdated(int newModeratorPlayerRoomId);
    Task RoomRoleSettingsUpdated();

    Task PlayerKicked(int kickedPlayerId);
}