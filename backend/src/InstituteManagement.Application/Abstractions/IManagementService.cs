using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface IManagementService
{
    Task<IReadOnlyList<CatalogItemDto>> GetAsync(string resource, string? search, Guid? departmentId, CancellationToken cancellationToken);
    Task<CatalogItemDto> CreateAsync(string resource, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<CatalogItemDto> UpdateAsync(string resource, Guid id, Dictionary<string, string> values, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string resource, Guid id, CancellationToken cancellationToken);
}
