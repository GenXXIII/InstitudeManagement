using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Application.Features.Dashboard.GetDashboard;

namespace InstituteManagement.Application.Tests.Dashboard;

public sealed class GetDashboardHandlerTests
{
    [Fact]
    public async Task Handle_returns_query_service_snapshot()
    {
        var expected = new DashboardDto([], 94.2m, 1.2m, [], [], [], [], [], [], 82.4m, []);
        var handler = new GetDashboardHandler(new FakeDashboardService(expected));

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Same(expected, result);
    }

    private sealed class FakeDashboardService(DashboardDto dashboard) : IDashboardQueryService
    {
        public Task<DashboardDto> GetAsync(CancellationToken cancellationToken) => Task.FromResult(dashboard);
    }
}
