using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Detective() : Role()
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        var allPlayersExceptSelf = actionCheckDto.ActivePlayers.Where(x=>x.Id != actionCheckDto.CurrentPlayer.Id && x.IsAlive).Select(x=>x.Id).ToList();
        var investigateAction = new RoleActionDto()
        {
            Label = "Investigate Player",
            Type = ActionType.Investigate,
            Enabled = true,
            ValidPlayerIds = allPlayersExceptSelf,
        };
        if (actionCheckDto.CurrentPlayer.IsAlive == false)
        {
            investigateAction.Enabled = false;
            investigateAction.DisabledReason = "Player is dead";
        }

        return [investigateAction];
    }
}