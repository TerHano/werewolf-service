using System.ComponentModel.DataAnnotations;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

public class RoomEntity
{
    [MaxLength(10)] public required string Id { get; init; }
    public Guid? CurrentModerator { get; set; }
    public GameState GameState { get; set; }

    public ICollection<PlayerRoomEntity>? PlayersInRooms { get; set; }
}