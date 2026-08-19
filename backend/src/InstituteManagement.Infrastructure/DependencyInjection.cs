using InstituteManagement.Application.Common;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace InstituteManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        if (bool.TryParse(configuration["Database:UseInMemory"], out var useInMemory) && useInMemory)
            services.AddDbContext<InstituteDbContext>(options => options.UseInMemoryDatabase("InstituteManagement"));
        else
        {
            services.AddDbContext<InstituteDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Database"), sql => sql.EnableRetryOnFailure()));
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnection))
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddScoped<IInstituteDataStore, InstituteDataStore>();
        return services;
    }
}
