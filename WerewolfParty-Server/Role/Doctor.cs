using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Doctor() : Role()
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {

        var healAction = new RoleActionDto()
        {
            Label = "Heal Player",
            Type = ActionType.Revive,
            Enabled = true,
        };
        if (actionCheckDto.CurrentPlayer.IsAlive == false)
        {
            healAction.Enabled = false;
            healAction.DisabledReason = "Player is dead";
        }
        return [healAction];
    }
}