using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.Features.Attendance.RecordAttendance;

namespace InstituteManagement.Application.Tests.Attendance;

public sealed class RecordAttendanceHandlerTests
{
    [Fact]
    public async Task Handle_persists_attendance_and_publishes_live_event()
    {
        var service = new FakeAttendanceService();
        var publisher = new FakePublisher();
        var handler = new RecordAttendanceHandler(service, publisher);
        var studentId = Guid.NewGuid();

        await handler.Handle(new RecordAttendanceCommand(studentId, "Late"), CancellationToken.None);

        Assert.Equal((studentId, "Late"), service.Attendance);
        Assert.Equal("ATTENDANCE_RECORDED", publisher.EventName);
    }

    private sealed class FakeAttendanceService : IAttendanceService
    {
        public (Guid, string)? Attendance { get; private set; }

        public Task RecordAsync(Guid studentId, string status, CancellationToken cancellationToken)
        {
            Attendance = (studentId, status);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePublisher : ILiveUpdatePublisher
    {
        public string? EventName { get; private set; }

        public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken)
        {
            EventName = eventName;
            return Task.CompletedTask;
        }
    }
}
