using System.Security.Claims;

namespace WerewolfParty_Server;

public static class Util
{
    public static Guid GetPlayerGuidFromHttpContext(HttpContext httpContext)
    {
        var playerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (playerId == null) throw new Exception("No player id found");
        return Guid.Parse(playerId);
    }
}