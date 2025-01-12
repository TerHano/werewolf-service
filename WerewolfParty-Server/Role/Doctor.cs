using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Doctor() : Role()
{
    public override List<RoleActionDto> GetActions(List<RoomGameActionEntity> actions, Guid playerId)
    {
        var healAction = new RoleActionDto()
        {
            Label = "Heal Player",
            Type = ActionType.Revive,
            Enabled = true,
        };
        return [healAction];
    }
}