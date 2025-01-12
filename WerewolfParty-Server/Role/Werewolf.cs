using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Werewolf() : Role()
{
    public override List<RoleActionDto> GetActions(List<RoomGameActionEntity> actions, Guid playerId)
    {
        var killAction = new RoleActionDto()
        {
            Label = "Kill Player",
            Type = ActionType.Kill,
            Enabled = true,
        };
        return [killAction];
    }
}