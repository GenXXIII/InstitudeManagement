using System.Text.Json;
using StackExchange.Redis;

namespace InstituteManagement.Infrastructure.Services.Common;

public sealed class InstituteCache(IConnectionMultiplexer? redis = null)
{
    private const string DashboardKey = "institute:dashboard:v3";

    public async Task<T?> ReadDashboardAsync<T>()
    {
        if (redis is null) return default;
        var value = await redis.GetDatabase().StringGetAsync(DashboardKey);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
    }

    public Task WriteDashboardAsync<T>(T value) => redis is null
        ? Task.CompletedTask
        : redis.GetDatabase().StringSetAsync(DashboardKey, JsonSerializer.Serialize(value), TimeSpan.FromSeconds(20));

    public Task InvalidateDashboardAsync() => redis is null
        ? Task.CompletedTask
        : redis.GetDatabase().KeyDeleteAsync(DashboardKey);
}
