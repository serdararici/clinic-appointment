using Clinic.Application.Availability;

namespace Clinic.Api;

/// <summary>
/// Keeps the booking window filled with the default work schedule: every few hours (configurable with
/// <c>DefaultSchedule:IntervalHours</c>, default 6) it extends the schedule of every automatic-schedule doctor forward.
/// The same method also runs once at startup (see Program.cs). A failed run is logged and retried at the next tick.
/// </summary>
public class DefaultScheduleWorker(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, IConfiguration configuration, ILogger<DefaultScheduleWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = configuration.GetValue("DefaultSchedule:IntervalHours", 6.0);
        var interval = TimeSpan.FromHours(hours > 0 ? hours : 6.0);

        using var timer = new PeriodicTimer(interval);
        while (await WaitAsync(timer, stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDefaultScheduleService>();
                var added = await service.EnsureForAllAsync(timeProvider.GetUtcNow().UtcDateTime, stoppingToken);
                logger.LogInformation("Default schedule check done: {Added} availability ranges added.", added);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Default schedule check failed; it will be retried at the next interval.");
            }
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
