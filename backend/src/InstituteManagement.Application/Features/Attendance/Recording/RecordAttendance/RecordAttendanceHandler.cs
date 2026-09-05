using InstituteManagement.Application.Common.LiveUpdates;
using InstituteManagement.Application.Features.Attendance;
using MediatR;

namespace InstituteManagement.Application.Features.Attendance.RecordAttendance;

public sealed class RecordAttendanceHandler(IAttendanceService service, ILiveUpdatePublisher publisher) : IRequestHandler<RecordAttendanceCommand>
{
    public async Task Handle(RecordAttendanceCommand request, CancellationToken cancellationToken)
    {
        await service.RecordAsync(request.StudentId, request.Status, cancellationToken);
        await publisher.PublishAsync("ATTENDANCE_RECORDED", new { request.StudentId, request.Status, RecordedAt = DateTime.UtcNow }, cancellationToken);
    }
}
