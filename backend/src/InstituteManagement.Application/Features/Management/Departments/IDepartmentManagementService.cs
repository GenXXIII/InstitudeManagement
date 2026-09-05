namespace InstituteManagement.Application.Features.Management.Departments;

public interface IDepartmentManagementService
{
    Task<IReadOnlyList<DepartmentResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<DepartmentResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<DepartmentResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
