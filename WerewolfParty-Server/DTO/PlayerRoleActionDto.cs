using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerRoleActionDto: PlayerDTO
{
    public RoleName Role { get; set; }
    public List<RoleActionDto> Actions { get; set; }
}