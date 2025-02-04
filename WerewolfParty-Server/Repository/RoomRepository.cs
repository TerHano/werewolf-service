using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;

namespace WerewolfParty_Server.Repository;

public class RoomRepository(WerewolfDbContext context)
{
    public List<RoomEntity> GetAllRooms()
    {
        return context.Rooms.ToList();
    }

    public async Task CreateRoom(RoomEntity newRoomEntity)
    {
        context.Rooms.Add(newRoomEntity);
        await context.SaveChangesAsync();
    }

    public int? GetModeratorForRoom(string roomId)
    {
        var room = GetRoom(roomId);
        return room.CurrentModerator;
    }

    public WinCondition GetWinConditionForRoom(string roomId)
    {
        var room = GetRoom(roomId);
        return room.WinCondition;
    }

    public bool DoesRoomExist(string roomId)
    {
        return context.Rooms.Any((room) => EF.Functions.ILike(room.Id, roomId));
    }

    public RoomEntity GetRoom(string roomId)
    {
        var room = context.Rooms.FirstOrDefault(room =>
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