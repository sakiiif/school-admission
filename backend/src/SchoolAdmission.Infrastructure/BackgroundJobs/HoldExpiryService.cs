using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Infrastructure.BackgroundJobs;

// Periodically marks Held rows past their expiry as Expired.
// Idempotent, so it is safe when several backend processes run it at the same time.
public sealed class HoldExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<HoldExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            // The repository is scoped (it uses DbContext), the service is a singleton,
            // so create a fresh scope for every run.
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAdmissionRepository>();

            var expired = await repository.ExpireDueHoldsAsync(ct);
            if (expired > 0)
                logger.LogInformation("Marked {Count} hold(s) as expired.", expired);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // let ExecuteAsync handle shutdown
        }
        catch (Exception ex)
        {
            // A DB hiccup must not kill the loop. Log it and try again on the next tick.
            logger.LogError(ex, "Failed to expire due holds. Will retry.");
        }
    }
}