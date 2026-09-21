namespace InstituteManagement.Application.Features.Attendance.ClassSessions;

public interface IClassPermissionService
{
    Task<ClassPermissionRequestDto> RequestAsync(Guid studentId, DateOnly sessionDate, string reason, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassPermissionRequestDto>> GetForStudentAsync(Guid studentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassPermissionRequestDto>> GetForTeacherAsync(Guid teacherId, CancellationToken cancellationToken);
    Task<ClassPermissionRequestDto> ReviewAsync(Guid requestId, Guid teacherId, string decision, CancellationToken cancellationToken);
}
