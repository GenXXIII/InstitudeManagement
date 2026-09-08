using InstituteManagement.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InstituteManagement.Infrastructure;

public static class InfrastructureStartup
{
    public static async Task InitializeInfrastructureAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        await DatabaseInitializer.InitializeAsync(
            scope.ServiceProvider.GetRequiredService<InstituteDbContext>(),
            cancellationToken);
    }
}
