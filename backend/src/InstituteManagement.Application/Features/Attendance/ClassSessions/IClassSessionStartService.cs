namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public interface IClassSessionStartService
{
    Task<ClassSessionStartDto> StartAsync(Guid scheduleEntryId, Guid teacherId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassSessionStartDto>> GetTodayAsync(Guid teacherId, CancellationToken cancellationToken);
}
