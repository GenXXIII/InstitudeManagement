using System.Text.Json;
using StackExchange.Redis;

namespace InstituteManagement.Infrastructure.Services.Common;

public sealed class InstituteCache(IConnectionMultiplexer? redis = null)
{
    private const string DashboardKeyPrefix = "institute:dashboard:v5";
    private static readonly string[] DashboardRanges = ["daily", "weekly", "monthly", "yearly", "all"];

    public async Task<T?> ReadDashboardAsync<T>(string range, CancellationToken cancellationToken)
    {
        if (redis is null) return default;
        var value = await redis.GetDatabase().StringGetAsync(DashboardKey(range)).WaitAsync(cancellationToken);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
    }

    public Task WriteDashboardAsync<T>(string range, T value, CancellationToken cancellationToken) => redis is null
        ? Task.CompletedTask
        : redis.GetDatabase()
            .StringSetAsync(DashboardKey(range), JsonSerializer.Serialize(value), Expiration(range))
            .WaitAsync(cancellationToken);

    public Task InvalidateDashboardAsync(CancellationToken cancellationToken) => redis is null
        ? Task.CompletedTask
        : redis.GetDatabase().KeyDeleteAsync(DashboardRanges.Select(range => (RedisKey)DashboardKey(range)).ToArray()).WaitAsync(cancellationToken);

    private static string DashboardKey(string range) => $"{DashboardKeyPrefix}:{NormalizeRange(range)}";
    private static string NormalizeRange(string range) => DashboardRanges.Contains(range, StringComparer.OrdinalIgnoreCase) ? range.ToLowerInvariant() : "monthly";
    private static TimeSpan Expiration(string range) => NormalizeRange(range) switch
    {
        "daily" => TimeSpan.FromSeconds(30),
        "weekly" => TimeSpan.FromMinutes(1),
        "monthly" => TimeSpan.FromMinutes(2),
        "yearly" => TimeSpan.FromMinutes(3),
        _ => TimeSpan.FromMinutes(5)
    };
}
