using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class RoleActionDto
{
    public required string Label { get; set; }
    public required ActionType Type { get; set; }
    public required bool Enabled { get; set; }
    public string? DisabledReason { get; set; }
    
    public required List<int> ValidPlayerIds { get; set; }
}