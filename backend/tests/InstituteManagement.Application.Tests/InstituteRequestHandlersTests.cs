using InstituteManagement.Application.Common;
using InstituteManagement.Application.Features;

namespace InstituteManagement.Application.Tests;

public sealed class InstituteRequestHandlersTests
{
    [Fact]
    public async Task Dashboard_query_returns_store_snapshot()
    {
        var expected = new DashboardDto([], 94.2m, 1.2m, [], [], [], [], [], [], []);
        var store = new FakeStore { Dashboard = expected };
        var handler = new InstituteRequestHandlers(store, new FakePublisher());

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Attendance_command_persists_and_publishes_live_event()
    {
        var store = new FakeStore(); var publisher = new FakePublisher(); var handler = new InstituteRequestHandlers(store, publisher); var studentId = Guid.NewGuid();

        await handler.Handle(new RecordAttendanceCommand(studentId, "Late"), CancellationToken.None);

        Assert.Equal((studentId, "Late"), store.Attendance);
        Assert.Equal("ATTENDANCE_RECORDED", publisher.EventName);
    }

    private sealed class FakePublisher : ILiveUpdatePublisher
    {
        public string? EventName { get; private set; }
        public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken) { EventName = eventName; return Task.CompletedTask; }
    }

    private sealed class FakeStore : IInstituteDataStore
    {
        public DashboardDto Dashboard { get; init; } = new([], 0, 0, [], [], [], [], [], [], []);
        public (Guid, string)? Attendance { get; private set; }
        public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken) => Task.FromResult(Dashboard);
        public Task RecordAttendanceAsync(Guid studentId, string status, CancellationToken cancellationToken) { Attendance = (studentId, status); return Task.CompletedTask; }
        public Task<OperationDto> GetOperationAsync(string module, Guid? departmentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<RecordDto>> GetRecordsAsync(string? search, string? type, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<CatalogItemDto>> GetCatalogAsync(string resource, string? search, Guid? departmentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CatalogItemDto> CreateCatalogAsync(string resource, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<CatalogItemDto> UpdateCatalogAsync(string resource, Guid id, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> DeleteCatalogAsync(string resource, Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SettingsDto> GetSettingsAsync(string section, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<SettingsDto> SaveSettingsAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SubmitGradeAsync(Guid studentId, Guid courseId, decimal score, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
