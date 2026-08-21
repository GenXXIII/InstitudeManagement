using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Infrastructure.Services.Management;

public sealed class ManagementService(IEnumerable<IManagementFeature> features) : IManagementService
{
    private readonly IReadOnlyDictionary<string, IManagementFeature> _features = features.ToDictionary(x => x.Resource, StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<CatalogItemDto>> GetAsync(string resource, string? search, Guid? departmentId, CancellationToken ct) => Resolve(resource).GetAsync(search?.Trim(), departmentId, ct);
    public Task<CatalogItemDto> CreateAsync(string resource, Dictionary<string, string> values, CancellationToken ct) => Resolve(resource).CreateAsync(values, ct);
    public Task<CatalogItemDto> UpdateAsync(string resource, Guid id, Dictionary<string, string> values, CancellationToken ct) => Resolve(resource).UpdateAsync(id, values, ct);
    public Task<bool> DeleteAsync(string resource, Guid id, CancellationToken ct) => Resolve(resource).DeleteAsync(id, ct);

    private IManagementFeature Resolve(string resource) => _features.GetValueOrDefault(resource) ?? throw new ArgumentException($"Management feature '{resource}' is not supported.");
}
