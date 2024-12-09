using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Models;

namespace WerewolfParty_Server.Hubs;

public interface IClientEventsHub
{
    Task GameState(GameState gameState);
    Task PlayerNameUpdated(PlayerDTO player);
    Task PlayersInLobbyUpdated();
    Task ModeratorUpdated();
}