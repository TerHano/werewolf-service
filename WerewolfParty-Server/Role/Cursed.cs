using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Cursed() : Role()
{
    public override List<RoleActionDto> GetActions(List<RoomGameActionEntity> actions, Guid playerId)
    {
        return [];
    }
}