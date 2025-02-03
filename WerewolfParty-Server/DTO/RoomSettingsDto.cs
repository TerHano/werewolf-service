using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class RoomSettingsDto
{
    public int Id { get; set; }
    public required int NumberOfWerewolves { get; set; }
    public List<RoleName> SelectedRoles { get; set; } = new();
    public bool ShowGameSummary { get; set; }
    public bool AllowMultipleSelfHeals { get; set; }
}