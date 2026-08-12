using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

public class PlayerRoleDTO
{
    public int Id { get; set; }
    public string Nickname { get; set; }
    public RoleName Role { get; set; }
}