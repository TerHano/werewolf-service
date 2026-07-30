namespace WerewolfParty_Server.Models.Request;

/// <summary>
/// Implemented by any request body that identifies the room it acts on, so that
/// <see cref="WerewolfParty_Server.Filters.RoomAccessFilter"/> can resolve the room
/// without knowing the concrete request type.
/// </summary>
public interface IRoomScopedRequest
{
    string RoomId { get; }
}
