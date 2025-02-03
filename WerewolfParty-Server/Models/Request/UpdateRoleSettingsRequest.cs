using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Models.Request;

public class UpdateRoleSettingsRequest : RoomIdRequest
{
    public int Id { get; set; }
    public int NumberOfWerewolves { get; set; }
    public List<RoleName> SelectedRoles { get; set; }
    public bool ShowGameSummary { get; set; }
    public bool AllowMultipleSelfHeals { get; set; }
}