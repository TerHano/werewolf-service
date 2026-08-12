namespace WerewolfParty_Server.DTO;

/// <summary>
/// A browser PushSubscription, flattened. Mirrors what
/// <c>PushManager.subscribe()</c> hands back: the endpoint plus the two encryption keys.
///
/// Carries no player id — the caller is identified by their token, so nobody can register a
/// device against somebody else's notifications.
/// </summary>
public class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}
