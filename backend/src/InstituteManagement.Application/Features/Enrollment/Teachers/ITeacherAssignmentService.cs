using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers;

public interface ITeacherAssignmentService
{
    Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid teacherId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid teacherId, CancellationToken cancellationToken);
}
