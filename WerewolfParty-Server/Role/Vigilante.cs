using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Vigilante() : Role()
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        var currentPlayer = actionCheckDto.CurrentPlayer;
        var queuedActions = actionCheckDto.QueuedActions;
        var killAction = new RoleActionDto()
        {
            Label = "Kill Player",
            Type = ActionType.VigilanteKill,
            Enabled = true,
        };
        if (actionCheckDto.CurrentPlayer.isAlive == false)
        {
            killAction.Enabled = false;
            killAction.DisabledReason = "Player is dead";
            
        }
        else
        {
            //Check if vigilante is due for suicide
            var hasSuicide = queuedActions.Any((x) =>
                x.Action.Equals(ActionType.Suicide) &&
                (x.PlayerId == currentPlayer.PlayerGuid));
            if (hasSuicide)
            {
                killAction.Enabled = false;
                killAction.DisabledReason = "The Vigilante is guilt ridden and has decided to take his life tonight";
            }
        }

        return [killAction];
    }
}