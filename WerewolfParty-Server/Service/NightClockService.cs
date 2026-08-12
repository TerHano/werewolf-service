namespace WerewolfParty_Server.Service;

/// <summary>
/// Drives the night forward. Since a night step always runs to its deadline and nothing a
/// client does can shorten it, something server-side has to notice that the time is up — a
/// lazy check on the next request would stall a room where nobody is touching the app, which is
/// exactly what a room full of people with their eyes closed looks like.
///
/// One tick a second is plenty: the step lengths are tens of seconds, and a second of slop is
/// invisible next to a countdown people are reading off a phone.
///
/// This assumes a single server instance, which is how the app is deployed today. If it ever
/// scales out, every instance will run this loop and race on the same expired rooms — that is
/// already safe, because each transition goes through the guarded update in
/// <see cref="Repository.RoomRepository.TryMoveToNightStep"/> and only one caller can win.
/// </summary>
public class NightClockService(IServiceScopeFactory scopeFactory, ILogger<NightClockService> logger)
    : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Night clock started");
        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // The engine and its repositories are scoped, so each tick gets its own scope
                // and its own DbContext rather than sharing one across the app's lifetime.
                using var scope = scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<NightEngineService>();
                await engine.AdvanceExpiredRooms();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                // Never let a bad tick kill the loop — the whole night stops if this task ends.
                logger.LogError(e, "Night clock tick failed");
            }
        }

        logger.LogInformation("Night clock stopped");
    }
}
