using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Infrastructure.Services.Management;

public interface IManagementFeature
{
    string Resource { get; }
    Task<IReadOnlyList<CatalogItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct);
    Task<CatalogItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct);
    Task<CatalogItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
