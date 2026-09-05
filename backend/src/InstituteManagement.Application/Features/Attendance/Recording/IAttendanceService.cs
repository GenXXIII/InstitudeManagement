namespace InstituteManagement.Application.Features.Attendance;

public interface IAttendanceService
{
    Task RecordAsync(Guid studentId, string status, CancellationToken cancellationToken);
}
