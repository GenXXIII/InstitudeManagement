using InstituteManagement.Application.Features.Record;

namespace InstituteManagement.Infrastructure.Services.History;

public interface IHistorySnapshotProvider
{
    string Type { get; }
    Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken);
}
