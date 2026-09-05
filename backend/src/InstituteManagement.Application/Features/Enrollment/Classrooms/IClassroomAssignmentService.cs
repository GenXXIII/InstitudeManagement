using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms;

public interface IClassroomAssignmentService
{
    Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid classroomId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid classroomId, CancellationToken cancellationToken);
}
