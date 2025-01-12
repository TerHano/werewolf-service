using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Detective() : Role()
{
    public override List<RoleActionDto> GetActions(List<RoomGameActionEntity> actions, Guid playerId)
    {
        var investigateAction = new RoleActionDto()
        {
            Label = "Investigate Player",
            Type = ActionType.Investigate,
            Enabled = true,
        };
        return [investigateAction];
    }
}