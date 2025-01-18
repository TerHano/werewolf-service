using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Detective() : Role()
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        
        var investigateAction = new RoleActionDto()
        {
            Label = "Investigate Player",
            Type = ActionType.Investigate,
            Enabled = true,
        };
        if (actionCheckDto.CurrentPlayer.isAlive == false)
        {
            investigateAction.Enabled = false;
            investigateAction.DisabledReason = "Player is dead";
            
        }
        return [investigateAction];
    }
}