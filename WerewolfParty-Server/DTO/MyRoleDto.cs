using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.DTO;

/// <summary>
/// The caller's own card. Carries the <c>player_role</c> id, which a player otherwise has no
/// way to learn — `assigned-role` returns only the role name, and the id on `player_room` is a
/// different one — yet every game action is addressed by it.
/// </summary>
public class MyRoleDto
{
    public int PlayerRoleId { get; set; }
    public RoleName Role { get; set; }
    public bool IsAlive { get; set; }
    public int NightKilled { get; set; }
}
