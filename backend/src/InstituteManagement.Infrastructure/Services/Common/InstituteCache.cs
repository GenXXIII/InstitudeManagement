using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InstituteManagement.Infrastructure.Services.Common;

public sealed class InstituteCache(IConnectionMultiplexer? redis = null, ILogger<InstituteCache>? logger = null)
{
    private const string DashboardKeyPrefix = "institute:dashboard:v8";
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromMilliseconds(750);
    private static readonly string[] DashboardRanges = ["daily", "weekly", "monthly", "yearly", "all"];
    private static readonly Meter CacheMeter = new("InstituteManagement.Cache", "1.0.0");
    private static readonly Counter<long> CacheOperations = CacheMeter.CreateCounter<long>("institute.cache.operations");
    private static readonly Histogram<double> CacheDuration = CacheMeter.CreateHistogram<double>("institute.cache.duration", "ms");
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _dashboardFlights = new(StringComparer.Ordinal);
    private int _recoveryInvalidationRequired;

    public async Task<T> GetOrCreateDashboardAsync<T>(
        string range,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken) where T : class
    {
        var normalizedRange = NormalizeRange(range);
        var cached = await ReadDashboardAsync<T>(normalizedRange, cancellationToken);
        if (cached is not null) return cached;

        var key = DashboardKey(normalizedRange);
        var flight = _dashboardFlights.GetOrAdd(key, _ => new Lazy<Task<object>>(
            async () =>
            {
                var secondRead = await ReadDashboardAsync<T>(normalizedRange, CancellationToken.None);
                if (secondRead is not null) return secondRead;

                var value = await factory(CancellationToken.None);
                await WriteDashboardAsync(normalizedRange, value, CancellationToken.None);
                return value;
            },
            LazyThreadSafetyMode.ExecutionAndPublication));
        var task = flight.Value;
        _ = task.ContinueWith(
            (_, state) =>
            {
                var (flights, flightKey, expected) = ((ConcurrentDictionary<string, Lazy<Task<object>>>, string, Lazy<Task<object>>))state!;
                flights.TryRemove(new KeyValuePair<string, Lazy<Task<object>>>(flightKey, expected));
            },
            (_dashboardFlights, key, flight),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return (T)await task.WaitAsync(cancellationToken);
    }

    public async Task<T?> ReadDashboardAsync<T>(string range, CancellationToken cancellationToken)
    {
        if (!CanUseRedis("read")) return default;
        var started = Stopwatch.GetTimestamp();
        try
        {
            await ClearRecoveredDashboardCacheAsync(cancellationToken);
            var value = await redis!.GetDatabase()
                .StringGetAsync(DashboardKey(range))
                .WaitAsync(OperationTimeout, cancellationToken);
            var outcome = value.IsNullOrEmpty ? "miss" : "hit";
            Record("read", outcome, started);
            logger?.LogDebug("Dashboard cache {CacheOutcome} for range {Range}.", outcome, NormalizeRange(range));
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsRecoverableCacheException(exception))
        {
            RecordFailure("read", started, exception);
            return default;
        }
    }

    public async Task WriteDashboardAsync<T>(string range, T value, CancellationToken cancellationToken)
    {
        if (!CanUseRedis("write")) return;
        var started = Stopwatch.GetTimestamp();
        try
        {
            await redis!.GetDatabase()
                .StringSetAsync(DashboardKey(range), JsonSerializer.Serialize(value), Expiration(range))
                .WaitAsync(OperationTimeout, cancellationToken);
            Record("write", "success", started);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsRecoverableCacheException(exception))
        {
            RecordFailure("write", started, exception);
        }
    }

    public async Task InvalidateDashboardAsync(CancellationToken cancellationToken)
    {
        if (!CanUseRedis("invalidate")) return;
        var started = Stopwatch.GetTimestamp();
        try
        {
            await DeleteDashboardKeysAsync(cancellationToken);
            Record("invalidate", "success", started);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsRecoverableCacheException(exception))
        {
            RecordFailure("invalidate", started, exception);
        }
    }

    private bool CanUseRedis(string operation)
    {
        if (redis is not null && redis.IsConnected) return true;
        Interlocked.Exchange(ref _recoveryInvalidationRequired, 1);
        CacheOperations.Add(1, Tags(operation, "bypass"));
        logger?.LogDebug("Dashboard cache {CacheOperation} bypassed because Redis is unavailable.", operation);
        return false;
    }

    private async Task ClearRecoveredDashboardCacheAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _recoveryInvalidationRequired, 0, 1) != 1) return;
        try
        {
            await DeleteDashboardKeysAsync(cancellationToken);
            CacheOperations.Add(1, Tags("recovery", "success"));
            logger?.LogInformation("Redis reconnected; potentially stale dashboard cache entries were cleared.");
        }
        catch
        {
            Interlocked.Exchange(ref _recoveryInvalidationRequired, 1);
            throw;
        }
    }

    private Task DeleteDashboardKeysAsync(CancellationToken cancellationToken) => redis!.GetDatabase()
        .KeyDeleteAsync(DashboardRanges.Select(range => (RedisKey)DashboardKey(range)).ToArray())
        .WaitAsync(OperationTimeout, cancellationToken);

    private void RecordFailure(string operation, long started, Exception exception)
    {
        Interlocked.Exchange(ref _recoveryInvalidationRequired, 1);
        Record(operation, "failure", started);
        logger?.LogWarning(exception, "Dashboard cache {CacheOperation} failed; SQL remains the source of truth.", operation);
    }

    private static void Record(string operation, string outcome, long started)
    {
        var tags = Tags(operation, outcome);
        CacheOperations.Add(1, tags);
        CacheDuration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, tags);
    }

    private static TagList Tags(string operation, string outcome) => new()
    {
        { "cache.name", "dashboard" },
        { "cache.operation", operation },
        { "cache.outcome", outcome }
    };

    private static bool IsRecoverableCacheException(Exception exception) => exception is
        RedisException or
        TimeoutException or
        JsonException or
        InvalidOperationException or
        ObjectDisposedException;

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
