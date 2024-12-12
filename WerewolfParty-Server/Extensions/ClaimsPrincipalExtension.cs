using System.Security.Claims;

namespace WerewolfParty_Server.Extensions;

public static class ClaimsPrincipalExtension
{
    public static Guid GetPlayerId(this ClaimsPrincipal claimsPrincipal)
    {
        var playerId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (playerId == null) throw new Exception("No player id found");
        return Guid.Parse(playerId);
    }
}