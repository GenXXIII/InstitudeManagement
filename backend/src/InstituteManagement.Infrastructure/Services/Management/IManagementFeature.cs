using InstituteManagement.Application.DTOs.Management;

namespace InstituteManagement.Infrastructure.Services.Management;

public interface IManagementFeature
{
    string Resource { get; }
    Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct);
    Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct);
    Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
