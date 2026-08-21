using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface IHistoryQueryService
{
    Task<IReadOnlyList<RecordDto>> GetAsync(string? search, string? type, CancellationToken cancellationToken);
}
