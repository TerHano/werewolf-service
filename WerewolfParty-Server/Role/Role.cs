using WerewolfParty_Server.DTO;

namespace WerewolfParty_Server.Role;

public abstract class Role
{
    public abstract List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto);
}