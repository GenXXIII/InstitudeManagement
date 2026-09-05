using InstituteManagement.Application.Features.Record;

namespace InstituteManagement.Application.Features.History;

public interface IHistoryQueryService
{
    Task<IReadOnlyList<RecordDto>> GetAsync(string? search, string? type, CancellationToken cancellationToken);
}
