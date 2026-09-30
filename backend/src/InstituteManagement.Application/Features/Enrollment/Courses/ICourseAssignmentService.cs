using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Courses;

public interface ICourseAssignmentService
{
    Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid courseId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid courseId, CancellationToken cancellationToken);
}
