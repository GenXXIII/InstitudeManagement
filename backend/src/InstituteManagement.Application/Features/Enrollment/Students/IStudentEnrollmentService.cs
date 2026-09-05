using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Students;

public interface IStudentEnrollmentService
{
    Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid studentId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid studentId, CancellationToken cancellationToken);
}
