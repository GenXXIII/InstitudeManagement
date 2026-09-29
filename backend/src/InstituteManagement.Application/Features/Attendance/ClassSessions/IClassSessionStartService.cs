namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public interface IClassSessionStartService
{
    Task<ClassSessionStartDto> StartAsync(Guid scheduleEntryId, Guid teacherId, CancellationToken cancellationToken);
    Task<ClassSessionStartDto> StartWithAttendanceQrAsync(Guid scheduleEntryId, Guid teacherId, string qrPayload, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassSessionStartDto>> GetTodayAsync(Guid teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassSessionStartDto>> GetTodayForStudentAsync(Guid studentId, CancellationToken cancellationToken);
}
