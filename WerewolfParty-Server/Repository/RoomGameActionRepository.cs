using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Repository;

public class RoomGameActionRepository(WerewolfDbContext context)
{
    public async Task QueueActionForPlayer(RoomGameActionEntity roomGameActionEntity)
    {
        if (roomGameActionEntity.Id == 0)
        {
            await context.RoomGameActions.AddAsync(roomGameActionEntity);
        }
        else
        {
            context.RoomGameActions.Update(roomGameActionEntity);
        }

        await context.SaveChangesAsync();
    }

    public async Task DequeueActionForPlayer(int actionId)
    {
        var playerAction = await context.RoomGameActions.FirstOrDefaultAsync(x =>
            x.Id.Equals(actionId));
        if (playerAction == null) return;
        context.RoomGameActions.Remove(playerAction);
        await context.SaveChangesAsync();
    }

    public async Task<RoomGameActionEntity?> GetQueuedPlayerActionForRoom(string roomId, int playerRoleId)
    {
        var playerAction = await context.RoomGameActions
            .FirstOrDefaultAsync(x => EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Queued &&
                                 x.PlayerRoleId.Equals(playerRoleId));

        return playerAction;
    }

    public async Task<RoomGameActionEntity?> GetQueuedWerewolfActionForRoom(string roomId)
    {
        var playerAction = await context.RoomGameActions.FirstOrDefaultAsync(x =>
            EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Queued &&
            x.Action == ActionType.WerewolfKill);
        return playerAction;
    }

    public async Task<List<RoomGameActionEntity>> GetAllQueuedActionsForRoom(string roomId)
    {
        return await context.RoomGameActions.Where(x =>
                EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Queued)
            .ToListAsync();
    }

    public async Task<List<RoomGameActionEntity>> GetAllProcessedActionsForRoom(string roomId, bool includeDependencies = false)
    {
        if (includeDependencies)
        {
            return await context.RoomGameActions.Include((r) => r.PlayerRole).ThenInclude((pr => pr.PlayerRoom))
                .Include((r) => r.AffectedPlayerRole).ThenInclude((pr => pr.PlayerRoom)).Where(x =>
                    EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Processed)
                .ToListAsync();
        }

        return await  context.RoomGameActions.Where(x =>
                EF.Functions.ILike(x.RoomId, roomId) && x.State == ActionState.Processed)
            .ToListAsync();
    }


    public async Task MarkActionsAsProcessed(string roomId, List<RoomGameActionEntity> actions)
    {
        foreach (var action in actions)
        {
            action.State = ActionState.Processed;
            context.Update(action);
        }
        await context.SaveChangesAsync();
    }


    public async Task ClearAllActionsForRoom(string roomId)
    {
        context.RoomGameActions.RemoveRange(context.RoomGameActions.Where(x =>
            EF.Functions.ILike(x.RoomId, roomId)));
       await context.SaveChangesAsync();
    }
}

