using InstituteManagement.Application.DTOs.Enrollment;

namespace InstituteManagement.Application.Abstractions;

public interface IEnrollmentService
{
    Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string resource, string? search, Guid? departmentId, int? year, CancellationToken cancellationToken);
    Task<EnrollmentItemDto> UpdateAsync(string resource, Guid resourceId, Dictionary<string, string> values, CancellationToken cancellationToken);
}
