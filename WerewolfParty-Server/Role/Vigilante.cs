using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Vigilante : Role
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        var allPlayersExceptSelf = actionCheckDto.ActivePlayers.Where(x=>x.Id != actionCheckDto.CurrentPlayer.Id && x.IsAlive).Select(x=>x.Id).ToList();
        var currentPlayer = actionCheckDto.CurrentPlayer;
        var queuedActions = actionCheckDto.QueuedActions;
        var killAction = new RoleActionDto()
        {
            Label = "Kill Player",
            Type = ActionType.VigilanteKill,
            Enabled = true,
            ValidPlayerIds = allPlayersExceptSelf
        };
        if (actionCheckDto.CurrentPlayer.IsAlive == false)
        {
            killAction.Enabled = false;
            killAction.DisabledReason = "Player is dead";
        }
        else
        {
            //Check if vigilante is due for suicide
            var hasSuicide = queuedActions.Any((x) =>
                x.Action.Equals(ActionType.Suicide) &&
                (x.PlayerRoleId == currentPlayer.Id));
            if (hasSuicide)
            {
                killAction.Enabled = false;
                killAction.DisabledReason = "The Vigilante is guilt ridden and has decided to take his life tonight";
            }
        }

        return [killAction];
    }
}