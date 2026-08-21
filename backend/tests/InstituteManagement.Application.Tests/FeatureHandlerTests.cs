using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Application.Features.Attendance.RecordAttendance;
using InstituteManagement.Application.Features.Dashboard.GetDashboard;

namespace InstituteManagement.Application.Tests;

public sealed class FeatureHandlerTests
{
    [Fact]
    public async Task Dashboard_handler_returns_query_service_snapshot()
    {
        var expected = new DashboardDto([], 94.2m, 1.2m, [], [], [], [], [], [], []);
        var handler = new GetDashboardHandler(new FakeDashboardService(expected));

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Attendance_handler_persists_and_publishes_live_event()
    {
        var service = new FakeAttendanceService();
        var publisher = new FakePublisher();
        var handler = new RecordAttendanceHandler(service, publisher);
        var studentId = Guid.NewGuid();

        await handler.Handle(new RecordAttendanceCommand(studentId, "Late"), CancellationToken.None);

        Assert.Equal((studentId, "Late"), service.Attendance);
        Assert.Equal("ATTENDANCE_RECORDED", publisher.EventName);
    }

    private sealed class FakeDashboardService(DashboardDto dashboard) : IDashboardQueryService
    {
        public Task<DashboardDto> GetAsync(CancellationToken cancellationToken) => Task.FromResult(dashboard);
    }

    private sealed class FakeAttendanceService : IAttendanceService
    {
        public (Guid, string)? Attendance { get; private set; }
        public Task RecordAsync(Guid studentId, string status, CancellationToken cancellationToken) { Attendance = (studentId, status); return Task.CompletedTask; }
    }

    private sealed class FakePublisher : ILiveUpdatePublisher
    {
        public string? EventName { get; private set; }
        public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken) { EventName = eventName; return Task.CompletedTask; }
    }
}
