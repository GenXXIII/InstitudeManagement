namespace InstituteManagement.Application.Features.Management.Students;

public interface IStudentManagementService
{
    Task<IReadOnlyList<StudentResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<StudentResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<StudentResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
