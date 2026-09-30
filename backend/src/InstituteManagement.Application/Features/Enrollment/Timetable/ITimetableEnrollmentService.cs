using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Timetable;

public interface ITimetableEnrollmentService
{
    Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid scheduleEntryId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid scheduleEntryId, CancellationToken cancellationToken);
}
