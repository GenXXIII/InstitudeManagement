using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class AcademicCalendarHostedService(IServiceScopeFactory scopeFactory, ILogger<AcademicCalendarHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ApplyAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await ApplyAsync(stoppingToken);
    }

    private async Task ApplyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AcademicCalendarRolloverService>().ApplyForCurrentDateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) { logger.LogError(exception, "Academic calendar rollover check failed."); }
    }
}
