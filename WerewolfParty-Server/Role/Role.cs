using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Role;

public abstract class Role()
{
    public abstract List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto);
}