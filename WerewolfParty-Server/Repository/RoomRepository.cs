using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;

namespace WerewolfParty_Server.Repository;

public class RoomRepository(WerewolfDbContext context)
{
    public async Task<List<RoomEntity>> GetAllRooms()
    {
        return await context.Rooms.ToListAsync();
    }

    public async Task CreateRoom(RoomEntity newRoomEntity)
    {
        await context.Rooms.AddAsync(newRoomEntity);
        await context.SaveChangesAsync();
    }

    public async Task<int?> GetModeratorForRoom(string roomId)
    {
        var room = await GetRoom(roomId);
        return room.CurrentModeratorId;
    }

    public async Task<WinCondition> GetWinConditionForRoom(string roomId)
    {
        var room = await GetRoom(roomId);
        return room.WinCondition;
    }

    public async Task<bool> DoesRoomExist(string roomId)
    {
        return await context.Rooms.AnyAsync((room) => EF.Functions.ILike(room.Id, roomId));
    }

    public async Task<RoomEntity> GetRoom(string roomId)
    {
        var room = await context.Rooms.FirstOrDefaultAsync(room =>
            EF.Functions.ILike(room.Id, roomId));
        if (room == null)
        {
            throw new RoomNotFoundException("RoomId does not exist");
        }
        return room;
    }

    public async Task UpdateRoom(RoomEntity roomEntity)
    {
        roomEntity.LastModifiedDate = DateTime.UtcNow;
        context.Rooms.Update(roomEntity);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Rooms whose current night step has run out of time. This is the engine's work queue.
    /// </summary>
    public async Task<List<string>> GetRoomIdsWithExpiredNightStep(DateTime now)
    {
        return await context.Rooms
            .Where(room => room.NightStep != null
                           && room.NightStepDeadline != null
                           && room.NightStepDeadline <= now)
            .Select(room => room.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Moves a room from one night step to another, but only if it is still on the step the
    /// caller believed it was on. Returns true when this call is the one that made the move.
    ///
    /// Every transition goes through here so that two callers racing — the clock firing at the
    /// same moment as a moderator extending the step, or two server instances scanning the
    /// same expired room — cannot both advance the night. The loser sees false and does
    /// nothing, including sending no broadcast.
    /// </summary>
    public async Task<bool> TryMoveToNightStep(string roomId, NightStep? expected, NightStep? next,
        DateTime? deadline)
    {
        var rowsChanged = await context.Rooms
            // Room ids are matched case-insensitively everywhere else, so do it here too rather
            // than depending on the caller having uppercased.
            .Where(room => EF.Functions.ILike(room.Id, roomId) && room.NightStep == expected)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(room => room.NightStep, next)
                .SetProperty(room => room.NightStepDeadline, deadline)
                .SetProperty(room => room.LastModifiedDate, DateTime.UtcNow));

        if (rowsChanged > 0)
        {
            // ExecuteUpdateAsync writes straight to the database and leaves the change tracker
            // holding the old values. Anything already tracking this room in the same scope —
            // notably GameService.ProgressToNextPoint, which reads the room and then writes
            // every column back — would otherwise resurrect the step we just moved away from,
            // leaving an expired step that the clock resolves again on its next tick, and
            // again after that. Refresh the tracked copy so the rest of the scope agrees with
            // the database.
            var tracked = context.ChangeTracker.Entries<RoomEntity>()
                .FirstOrDefault(entry =>
                    string.Equals(entry.Entity.Id, roomId, StringComparison.OrdinalIgnoreCase));
            if (tracked != null)
            {
                await tracked.ReloadAsync();
            }
        }

        return rowsChanged > 0;
    }
}