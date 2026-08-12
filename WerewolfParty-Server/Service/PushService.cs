using System.Text.Json;
using Lib.Net.Http.WebPush;
using WerewolfParty_Server.Repository;

namespace WerewolfParty_Server.Service;

/// <summary>
/// Sends Web Push notifications — the thing that makes a phone buzz while it is face-down on
/// the table with the app closed.
///
/// The payload is deliberately almost empty. A notification renders on a lock screen that the
/// person sitting next to you can read, so it says that it is your turn and nothing else: no
/// role, no target, no investigation result, no cause of death. Anything worth knowing is
/// behind opening the app.
///
/// Uses <c>Lib.Net.Http.WebPush</c> rather than the older <c>WebPush</c> package because that
/// one only implements the superseded <c>aesgcm</c> content encoding. RFC 8291 requires
/// <c>aes128gcm</c>, and Safari — the platform this feature exists for, since an iPhone on the
/// table is exactly the case the in-app prompt cannot cover — follows the RFC.
/// </summary>
public class PushService(
    PushSubscriptionRepository pushSubscriptionRepository,
    PushServiceClient pushServiceClient,
    IConfiguration configuration,
    ILogger<PushService> logger)
{
    public string? GetPublicKey() => configuration.GetValue<string>("Push:PublicKey");

    public bool IsConfigured() => !string.IsNullOrEmpty(GetPublicKey());

    /// <summary>
    /// Buzzes every device belonging to these players. Failures are swallowed on purpose — the
    /// night must not stall because a push service was slow or a subscription went stale.
    /// </summary>
    public async Task NotifyPlayers(List<Guid> playerIds, string roomId, string title, string body)
    {
        // Push is optional: a deployment without VAPID keys still runs the whole game, it just
        // relies on the in-app prompt. Not an error worth throwing on.
        if (!IsConfigured()) return;

        var subscriptions = await pushSubscriptionRepository.GetSubscriptionsForPlayers(playerIds);
        if (subscriptions.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            roomId,
            title,
            body
        });

        var deliveredIds = new List<int>();

        foreach (var subscription in subscriptions)
        {
            var pushSubscription = new PushSubscription { Endpoint = subscription.Endpoint };
            pushSubscription.SetKey(PushEncryptionKeyName.P256DH, subscription.P256dh);
            pushSubscription.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);

            try
            {
                await pushServiceClient.RequestPushMessageDeliveryAsync(pushSubscription,
                    new PushMessage(payload));
                deliveredIds.Add(subscription.Id);
            }
            catch (PushServiceClientException e) when (
                e.StatusCode == System.Net.HttpStatusCode.NotFound ||
                e.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                // 404/410 is how a push service says this subscription is dead — the app was
                // uninstalled, or permission revoked. Drop it rather than retrying forever.
                logger.LogInformation("Removing expired push subscription {Id}", subscription.Id);
                await pushSubscriptionRepository.RemoveByEndpoint(subscription.Endpoint);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to send push to subscription {Id}", subscription.Id);
            }
        }

        await pushSubscriptionRepository.TouchLastSeen(deliveredIds);
    }
}
