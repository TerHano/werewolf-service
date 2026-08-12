using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Role;

public class Doctor : Role
{
    public override List<RoleActionDto> GetActions(ActionCheckDto actionCheckDto)
    {
        var canHealSelf = true;
        var applicablePlayerIds = actionCheckDto.ActivePlayers.Where(x=>x.IsAlive).Select(x=>x.Id).ToList();
        var isPreventContinuousHealsOn = actionCheckDto.Settings.AllowMultipleSelfHeals == false;
        if (isPreventContinuousHealsOn)
        {
            var currentPlayerId = actionCheckDto.CurrentPlayer.Id;
            var previousHealsByPlayer = actionCheckDto.ProcessedActions
                .Where(x => x.PlayerRoleId == currentPlayerId && x.Action == ActionType.Revive).ToList();
            if (previousHealsByPlayer.Count != 0)
            {
                var lastHealsByPlayer = previousHealsByPlayer.MaxBy(x => x.Night);
                if (lastHealsByPlayer != null && lastHealsByPlayer.AffectedPlayerRoleId == currentPlayerId)
                {
                    canHealSelf = false;
                }
            }

            if (!canHealSelf)
            {
                applicablePlayerIds.Remove(currentPlayerId);
            }
        }

        var healAction = new RoleActionDto()
        {
            Label = "Heal Player",
            Type = ActionType.Revive,
            Enabled = true,
            ValidPlayerIds = applicablePlayerIds,
        };
        if (actionCheckDto.CurrentPlayer.IsAlive != false) return [healAction];
        healAction.Enabled = false;
        healAction.DisabledReason = "Player is dead";

        return [healAction];
    }
}