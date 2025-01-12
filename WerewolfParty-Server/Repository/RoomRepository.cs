using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Repository.Interface;

namespace WerewolfParty_Server.Repository;

public class RoomRepository(RoomDbContext context) : IRoomRepository
{
    public List<RoomEntity> GetAllRooms()
    {
        return context.Rooms.ToList();
    }

    public void CreateRoom(RoomEntity newRoomEntity)
    {
        context.Rooms.Add(newRoomEntity);
        context.SaveChanges();
    }

    public Guid GetModeratorForRoom(string roomId)
    {
        var room = GetRoom(roomId);
        return room.CurrentModerator;

    }

    public bool DoesRoomExist(string roomId)
    {
        return context.Rooms.Any((room) => room.Id.Equals(roomId, StringComparison.CurrentCultureIgnoreCase));
    }

    public RoomEntity GetRoom(string roomId)
    {
        var room = context.Rooms.FirstOrDefault(room => room.Id.Equals(roomId, StringComparison.CurrentCultureIgnoreCase));
        if (room == null)
        {
            throw new RoomNotFoundException("RoomId does not exist");
        }

        return room;
    }

    public void UpdateRoom(RoomEntity roomEntity)
    {
        context.Rooms.Update(roomEntity);
        context.SaveChanges();
    }
}