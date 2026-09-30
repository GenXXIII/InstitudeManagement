using InstituteManagement.Infrastructure.Services.Common;

namespace InstituteManagement.Infrastructure.Tests.Common;

public sealed class InstituteCacheTests
{
    [Fact]
    public async Task Concurrent_dashboard_misses_share_one_factory_execution()
    {
        var cache = new InstituteCache();
        var factoryCalls = 0;

        async Task<CacheValue> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref factoryCalls);
            await Task.Delay(75, cancellationToken);
            return new CacheValue("fresh");
        }

        var requests = Enumerable.Range(0, 20)
            .Select(_ => cache.GetOrCreateDashboardAsync("monthly", Factory, CancellationToken.None));
        var results = await Task.WhenAll(requests);

        Assert.Equal(1, factoryCalls);
        Assert.All(results, result => Assert.Equal("fresh", result.Value));
    }

    [Fact]
    public async Task Missing_redis_falls_back_to_factory_without_retaining_local_data()
    {
        var cache = new InstituteCache();
        var factoryCalls = 0;

        Task<CacheValue> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult(new CacheValue(factoryCalls.ToString()));
        }

        var first = await cache.GetOrCreateDashboardAsync("daily", Factory, CancellationToken.None);
        var second = await cache.GetOrCreateDashboardAsync("daily", Factory, CancellationToken.None);
        await cache.InvalidateDashboardAsync(CancellationToken.None);

        Assert.Equal("1", first.Value);
        Assert.Equal("2", second.Value);
        Assert.Equal(2, factoryCalls);
    }

    private sealed record CacheValue(string Value);
}
