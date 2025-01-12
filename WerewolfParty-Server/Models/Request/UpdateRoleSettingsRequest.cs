using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Models.Request;

public class UpdateRoleSettingsRequest : RoomIdRequest
{
    public int Id { get; set; }
    public NumberOfWerewolves Werewolves { get; set; }
    public List<RoleName> SelectedRoles { get; set; }
}