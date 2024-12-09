using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerRoleDTO
{
    public Guid Id { get; set; }
    public RoleName Role { get; set; }
}