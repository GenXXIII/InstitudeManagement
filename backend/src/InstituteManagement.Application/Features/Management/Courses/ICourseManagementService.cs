namespace InstituteManagement.Application.Features.Management.Courses;

public interface ICourseManagementService
{
    Task<IReadOnlyList<CourseResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<CourseResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<CourseResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
