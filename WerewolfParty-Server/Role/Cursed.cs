using WerewolfParty_Server.DTO;

namespace WerewolfParty_Server.Role;

public class Cursed : Role
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        return [];
    }
}