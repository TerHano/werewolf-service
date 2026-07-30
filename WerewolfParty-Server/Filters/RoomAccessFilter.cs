using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Models.Request;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.Filters;

/// <summary>
/// Verifies that the caller is actually a player in the room they are acting on, and
/// optionally that they are that room's moderator.
///
/// A valid token only proves the caller is *someone*; without this filter any authenticated
/// client could act on any room given its five character id.
/// </summary>
public class RoomAccessFilter(bool requireModerator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var roomId = ResolveRoomId(context);
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return Failure(StatusCodes.Status400BadRequest, "Room id is required.");
        }

        Guid playerGuid;
        try
        {
            playerGuid = httpContext.User.GetPlayerId();
        }
        catch (Exception)
        {
            return Failure(StatusCodes.Status401Unauthorized, "No player id found.");
        }

        var roomService = httpContext.RequestServices.GetRequiredService<RoomService>();

        PlayerDTO callingPlayer;
        try
        {
            callingPlayer = await roomService.GetPlayerInRoomUsingGuid(roomId, playerGuid);
        }
        catch (RoomNotFoundException)
        {
            return Failure(StatusCodes.Status404NotFound, "Room does not exist.");
        }
        catch (PlayerNotFoundException)
        {
            return Failure(StatusCodes.Status403Forbidden, "You are not a player in this room.");
        }

        if (requireModerator)
        {
            var moderator = await roomService.GetModeratorForRoom(roomId);
            if (moderator == null || moderator.Id != callingPlayer.Id)
            {
                return Failure(StatusCodes.Status403Forbidden, "You are not the moderator of this room.");
            }
        }

        return await next(context);
    }

    /// <summary>
    /// Room id comes either from the route (<c>/api/room/{roomId}/...</c>) or from a request
    /// body implementing <see cref="IRoomScopedRequest"/>.
    /// </summary>
    private static string? ResolveRoomId(EndpointFilterInvocationContext context)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is IRoomScopedRequest roomScopedRequest)
            {
                return roomScopedRequest.RoomId;
            }
        }

        return context.HttpContext.Request.RouteValues.TryGetValue("roomId", out var routeValue)
            ? routeValue?.ToString()
            : null;
    }

    private static IResult Failure(int statusCode, string message)
    {
        return TypedResults.Json(new APIResponse
        {
            Success = false,
            ErrorMessages = new List<string> { message }
        }, statusCode: statusCode);
    }
}
