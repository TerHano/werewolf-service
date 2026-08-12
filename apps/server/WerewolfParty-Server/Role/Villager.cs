using WerewolfParty_Server.DTO;

namespace WerewolfParty_Server.Role;

public class Villager : Role
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        return [];
    }
}