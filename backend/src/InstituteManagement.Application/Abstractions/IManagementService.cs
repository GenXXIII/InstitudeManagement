using InstituteManagement.Application.DTOs.Management;

namespace InstituteManagement.Application.Abstractions;

public interface IManagementService
{
    Task<IReadOnlyList<IManagementItemDto>> GetAsync(string resource, string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<IManagementItemDto> CreateAsync(string resource, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<IManagementItemDto> UpdateAsync(string resource, Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string resource, Guid id, CancellationToken cancellationToken);
}
