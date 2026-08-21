namespace InstituteManagement.Application.Abstractions;

public interface IAttendanceService
{
    Task RecordAsync(Guid studentId, string status, CancellationToken cancellationToken);
}
