using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms;

public interface IClassroomAssignmentService
{
    Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid classroomId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid classroomId, CancellationToken cancellationToken);
}
