using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerRoleActionDto 
{
    public int Id { get; set; }
    public required RoleName Role { get; set; }
    public required List<RoleActionDto> Actions { get; set; }
    
    public required bool isAlive { get; set; }
    public string Nickname { get; set; }
    public int AvatarIndex { get; set; }
}