namespace InstituteManagement.Application.Features.Management.Teachers;

public interface ITeacherManagementService
{
    Task<IReadOnlyList<TeacherResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<TeacherResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<TeacherResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
