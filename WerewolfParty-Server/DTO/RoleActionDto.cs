using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class RoleActionDto
{
    public string Label { get; set; }
    public ActionType Type { get; set; }
    public bool Enabled { get; set; }
    public string? DisabledReason { get; set; }
    
}