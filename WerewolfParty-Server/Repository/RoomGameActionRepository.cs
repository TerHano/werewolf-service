using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Repository;

public class RoomGameActionRepository(RoomGameActionDbContext context)
{
    public List<RoomGameActionEntity> GetQueuedActionsForRoom(string roomId)
    {
        return context.RoomGameActions.Where(x => x.RoomId.Equals(roomId, StringComparison.CurrentCultureIgnoreCase) && x.State == ActionState.Queued).ToList();
    }
    
    public List<RoomGameActionEntity> GetAllProcessedActionsForRoom(string roomId)
    {
        return context.RoomGameActions.Where(x => x.RoomId.Equals(roomId, StringComparison.CurrentCultureIgnoreCase) && x.State == ActionState.Processed).ToList();
    }

    public void ProcessActionsForRoom(string roomId, List<RoomGameActionEntity> actions)
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
        context.RoomGameActions.RemoveRange(context.RoomGameActions.Where(x => x.RoomId.Equals(roomId, StringComparison.CurrentCultureIgnoreCase)));
        context.SaveChanges();
    }
}