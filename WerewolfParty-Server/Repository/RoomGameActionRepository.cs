using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Repository;

public class RoomGameActionRepository(WerewolfDbContext context)
{
    public void QueueActionForPlayer(RoomGameActionEntity roomGameActionEntity)
    {
        if (roomGameActionEntity.Id == 0)
        {
            context.RoomGameActions.Add(roomGameActionEntity);
        }
        else
        {
            context.RoomGameActions.Update(roomGameActionEntity);
        }

        context.SaveChanges();
    }

    public void DequeueActionForPlayer(int actionId)
    {
        var playerAction = context.RoomGameActions.FirstOrDefault(x =>
            x.Id.Equals(actionId));
        if (playerAction == null) return;
        context.RoomGameActions.Remove(playerAction);
        context.SaveChanges();
    }

    public RoomGameActionEntity? GetQueuedPlayerActionForRoom(string roomId, int playerRoleId)
    {
        var playerAction = context.RoomGameActions
            .FirstOrDefault(x => EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Queued &&
                                 x.PlayerRoleId.Equals(playerRoleId));

        return playerAction;
    }

    public RoomGameActionEntity? GetQueuedWerewolfActionForRoom(string roomId)
    {
        var playerAction = context.RoomGameActions.FirstOrDefault(x =>
            EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Queued &&
            x.Action == ActionType.WerewolfKill);
        return playerAction;
    }

    public List<RoomGameActionEntity> GetAllQueuedActionsForRoom(string roomId)
    {
        return context.RoomGameActions.Where(x =>
                EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Queued)
            .ToList();
    }

    public List<RoomGameActionEntity> GetAllProcessedActionsForRoom(string roomId, bool includeDependencies = false)
    {
        if (includeDependencies)
        {
            return context.RoomGameActions.Include((r) => r.PlayerRole).ThenInclude((pr => pr.PlayerRoom))
                .Include((r) => r.AffectedPlayerRole).ThenInclude((pr => pr.PlayerRoom)).Where(x =>
                    EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Processed)
                .ToList();
        }

        return context.RoomGameActions.Where(x =>
                EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Processed)
            .ToList();
    }


    public void MarkActionsAsProcessed(string roomId, List<RoomGameActionEntity> actions)
    {
        foreach (var action in actions)
        {
            action.State = ActionState.Processed;
            context.Update(action);
        }

        context.SaveChanges();
    }


    public void ClearAllActionsForRoom(string roomId)
    {
        context.RoomGameActions.RemoveRange(context.RoomGameActions.Where(x =>
            EF.Functions.ILike(x.RoomId, roomId)));
        context.SaveChanges();
    }
}