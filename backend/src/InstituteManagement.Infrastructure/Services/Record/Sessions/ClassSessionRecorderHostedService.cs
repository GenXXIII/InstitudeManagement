using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassSessionRecorderHostedService(IServiceScopeFactory scopeFactory, ILogger<ClassSessionRecorderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecordAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await RecordAsync(stoppingToken);
    }

    private async Task RecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var count = await scope.ServiceProvider.GetRequiredService<ClassSessionRecorderService>().RecordCompletedForCurrentTimeAsync(cancellationToken);
            if (count > 0) logger.LogInformation("Recorded {Count} completed timetable sessions.", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogError(exception, "Completed timetable session recording failed."); }
    }
}
