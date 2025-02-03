using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DTO;

public class GameNightHistoryDTO
{
    public int Night { get; set; }
    public List<PlayerGameActionDTO> NightActions { get; set; }
    public List<PlayerGameActionDTO> DayActions { get; set; }
}