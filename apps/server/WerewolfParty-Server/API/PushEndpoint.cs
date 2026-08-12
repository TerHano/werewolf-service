using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Repository;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.API;

public static class PushEndpoint
{
    public static void RegisterPushEndpoints(this WebApplication app)
    {
        app.MapGet("/api/push/vapid-key", (PushService pushService) =>
            {
                // The public key is public by definition — the browser needs it to subscribe.
                return TypedResults.Ok(new APIResponse<string?>()
                {
                    Success = true,
                    Data = pushService.GetPublicKey()
                });
            })
            .WithName("GetVapidKey")
            .WithTags("Push")
            .WithSummary("Get the server's VAPID public key.")
            .WithDescription(
                "Needed by the browser to create a push subscription. Null when the deployment has no push keys configured, in which case clients fall back to the in-app prompt.")
            .RequireAuthorization();

        app.MapPost("/api/push/subscribe",
                async (HttpContext httpContext, PushSubscriptionRepository repository,
                    PushSubscriptionRequest request) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Endpoint) ||
                        string.IsNullOrWhiteSpace(request.P256dh) ||
                        string.IsNullOrWhiteSpace(request.Auth))
                    {
                        return TypedResults.Json(new APIResponse()
                        {
                            Success = false,
                            ErrorMessages = new List<string> { "Incomplete push subscription." }
                        }, statusCode: StatusCodes.Status400BadRequest);
                    }

                    // Bound to the caller's own token, never to an id in the body — otherwise
                    // anyone could point another player's notifications at their own device.
                    var playerGuid = httpContext.User.GetPlayerId();
                    await repository.Upsert(playerGuid, request.Endpoint, request.P256dh, request.Auth);

                    return TypedResults.Json(new APIResponse() { Success = true });
                })
            .WithName("SubscribeToPush")
            .WithTags("Push")
            .WithSummary("Register this device for turn notifications.")
            .WithDescription("Stores the browser's push subscription against the calling player.")
            .RequireAuthorization();

        app.MapDelete("/api/push/subscribe",
                async (PushSubscriptionRepository repository, string endpoint) =>
                {
                    await repository.RemoveByEndpoint(endpoint);
                    return TypedResults.Ok(new APIResponse() { Success = true });
                })
            .WithName("UnsubscribeFromPush")
            .WithTags("Push")
            .WithSummary("Stop sending notifications to this device.")
            .WithDescription("Removes a stored push subscription by its endpoint.")
            .RequireAuthorization();
    }
}
