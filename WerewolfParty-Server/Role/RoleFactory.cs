using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class RoleFactory
{
    public Role GetRole(RoleName roleName)
    {
        switch (roleName)
        {
            case RoleName.Doctor:
                return new Doctor();
            case RoleName.WereWolf:
                return new Werewolf();
            case RoleName.Detective:
                return new Detective();
            case RoleName.Drunk:
                return new Drunk();
            case RoleName.Witch:
                return new Witch();
            case RoleName.Vigilante:
                return new Vigilante();
            case RoleName.Villager:
                return new Villager();
            case RoleName.Cursed:
                return new Cursed();

            default:
                throw new Exception($"Role {roleName} not found");
        }
    }
}