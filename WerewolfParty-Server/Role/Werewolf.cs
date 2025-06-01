using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Werewolf : Role
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        var allPlayersExceptSelf = actionCheckDto.ActivePlayers.Where(x=>x.Id != actionCheckDto.CurrentPlayer.Id && x.IsAlive).Select(x=>x.Id).ToList();
        var killAction = new RoleActionDto()
        {
            Label = "Kill Player",
            Type = ActionType.WerewolfKill,
            Enabled = true,
            ValidPlayerIds = allPlayersExceptSelf,
        };
        return [killAction];
    }
}