using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace WerewolfParty_Server.Service;

/// <summary>
/// Maps a hub connection to the player GUID carried in its token, so the server can address a
/// single player with <c>Clients.User(...)</c>.
///
/// This must agree with <see cref="Extensions.ClaimsPrincipalExtension.GetPlayerId"/>: the JWT
/// only ever carries a <see cref="ClaimTypes.NameIdentifier"/> claim, and nothing sets a
/// <see cref="ClaimTypes.Name"/> claim. Reading <c>Identity.Name</c> here therefore returned
/// null for every connection and user-targeted sends silently reached nobody.
///
/// The id is normalised to the canonical lowercase GUID form, so the key does not depend on how
/// the GUID happened to be formatted inside the token.
///
/// SignalR looks users up by ordinal string comparison, so <b>senders must match that exact
/// form</b>: call <c>Clients.User(playerGuid.ToString())</c>. <c>Guid.ToString()</c> always
/// produces canonical lowercase, so passing a Guid is safe; passing a hand-built or uppercased
/// string is not, and fails silently by reaching nobody.
/// </summary>
public class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var playerId = connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Anonymous connections are allowed — the hub itself does not require authorization —
        // so a missing claim is normal and simply means this connection cannot be targeted.
        if (playerId == null) return null;

        return Guid.TryParse(playerId, out var playerGuid)
            ? playerGuid.ToString()
            : null;
    }
}
