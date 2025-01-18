using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Drunk() : Role()
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        return [];
    }
}