using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Departments;

public interface IDepartmentEnrollmentService
{
    Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken);
}
