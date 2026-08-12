using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DbContext;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.Repository;

public class PushSubscriptionRepository(WerewolfDbContext context)
{
    public async Task<List<PushSubscriptionEntity>> GetSubscriptionsForPlayers(List<Guid> playerIds)
    {
        if (playerIds.Count == 0) return [];

        return await context.PushSubscriptions
            .Where(subscription => playerIds.Contains(subscription.PlayerId))
            .ToListAsync();
    }

    /// <summary>
    /// Stores a subscription, replacing any existing row for the same endpoint. Browsers hand
    /// out the same endpoint when a device re-subscribes, so upserting here is what stops one
    /// phone accumulating rows and receiving the same notification several times over.
    /// </summary>
    public async Task Upsert(Guid playerId, string endpoint, string p256dh, string auth)
    {
        var existing = await context.PushSubscriptions
            .FirstOrDefaultAsync(subscription => subscription.Endpoint == endpoint);

        if (existing != null)
        {
            // The endpoint can be reassigned to a different player — a shared phone, or someone
            // clearing their session and getting a fresh GUID.
            existing.PlayerId = playerId;
            existing.P256dh = p256dh;
            existing.Auth = auth;
            existing.LastSeenDate = DateTime.UtcNow;
        }
        else
        {
            await context.PushSubscriptions.AddAsync(new PushSubscriptionEntity
            {
                PlayerId = playerId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
                CreatedDate = DateTime.UtcNow,
                LastSeenDate = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task RemoveByEndpoint(string endpoint)
    {
        await context.PushSubscriptions
            .Where(subscription => subscription.Endpoint == endpoint)
            .ExecuteDeleteAsync();
    }

    public async Task TouchLastSeen(List<int> subscriptionIds)
    {
        if (subscriptionIds.Count == 0) return;

        await context.PushSubscriptions
            .Where(subscription => subscriptionIds.Contains(subscription.Id))
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(subscription => subscription.LastSeenDate, DateTime.UtcNow));
    }
}
