using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Witch : Role
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        var allPlayers = actionCheckDto.ActivePlayers.Where(x=>x.IsAlive).Select(x=>x.Id).ToList();
        var allPlayersExceptSelf = actionCheckDto.ActivePlayers.Where(x=>x.Id != actionCheckDto.CurrentPlayer.Id && x.IsAlive).Select(x=>x.Id).ToList();
        var currentPlayer = actionCheckDto.CurrentPlayer;
        var healAction = new RoleActionDto()
        {
            Label = "Heal Player",
            Type = ActionType.Revive,
            Enabled = true,
            ValidPlayerIds = allPlayers,
        };
        var killAction = new RoleActionDto()
        {
            Label = "Kill Player",
            Type = ActionType.Kill,
            Enabled = true,
            ValidPlayerIds = allPlayersExceptSelf,
        };

        if (currentPlayer.IsAlive == false)
        {
            killAction.Enabled = false;
            killAction.DisabledReason = "Player is dead";
            healAction.Enabled = false;
            healAction.DisabledReason = "Player is dead";
        }
        else
        {
            var priorActions = actionCheckDto.ProcessedActions;
            var hasKilled = priorActions.Any((action) =>
                action.PlayerRoleId.Equals(currentPlayer.Id) && action.Action.Equals(ActionType.Kill));
            var hasRevived = priorActions.Any((action) =>
                action.PlayerRoleId.Equals(currentPlayer.Id) && action.Action.Equals(ActionType.Revive));


            killAction.Enabled = !hasKilled;
            killAction.DisabledReason = hasKilled ? "Ability was previously used" : null;
            healAction.Enabled = !hasRevived;
            healAction.DisabledReason = hasRevived ? "Ability was previously used" : null;
        }

        return [healAction, killAction];
    }
}