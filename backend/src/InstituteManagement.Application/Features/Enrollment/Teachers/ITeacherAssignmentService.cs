using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers;

public interface ITeacherAssignmentService
{
    Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid teacherId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid teacherId, CancellationToken cancellationToken);
}
