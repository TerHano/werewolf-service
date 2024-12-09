using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Repository.Interface;

public interface IRoomRepository
{
    public List<RoomEntity> GetAllRooms();
    public bool DoesRoomExist(string roomId);
    public RoomEntity GetRoom(string roomId);
    public void CreateRoom(RoomEntity roomEntity);
    
    public void UpdateRoom(RoomEntity roomEntity);
}