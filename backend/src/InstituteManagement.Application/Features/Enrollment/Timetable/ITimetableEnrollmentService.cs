using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Timetable;

public interface ITimetableEnrollmentService
{
    Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(Guid scheduleEntryId, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(Guid scheduleEntryId, CancellationToken cancellationToken);
}
