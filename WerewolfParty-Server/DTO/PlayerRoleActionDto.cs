using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerRoleActionDto : PlayerDTO
{
    public required RoleName Role { get; set; }
    public required List<RoleActionDto> Actions { get; set; }
    
    public required bool isAlive { get; set; }
}