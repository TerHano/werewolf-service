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
}