namespace WerewolfParty_Server.Filters;

public static class RoomAccessFilterExtensions
{
    /// <summary>
    /// Requires a valid token AND that the caller is a player in the room being acted on.
    /// </summary>
    public static RouteHandlerBuilder RequireRoomMembership(this RouteHandlerBuilder builder)
    {
        return builder
            .RequireAuthorization()
            .AddEndpointFilter(new RoomAccessFilter(requireModerator: false));
    }

    /// <summary>
    /// Requires a valid token AND that the caller is the moderator of the room being acted on.
    /// Use for anything that changes the game or reveals hidden information.
    /// </summary>
    public static RouteHandlerBuilder RequireRoomModerator(this RouteHandlerBuilder builder)
    {
        return builder
            .RequireAuthorization()
            .AddEndpointFilter(new RoomAccessFilter(requireModerator: true));
    }
}
