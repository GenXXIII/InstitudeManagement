using InstituteManagement.Application.Features.History;
using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class HistoryQueryService(InstituteDbContext db, IEnumerable<IHistorySnapshotProvider> providers) : IHistoryQueryService
{
    private readonly IReadOnlyDictionary<string, IHistorySnapshotProvider> _providers = providers.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<RecordDto>> GetAsync(string? search, string? type, CancellationToken cancellationToken)
    {
        var requestedType = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim();
        var term = search?.Trim();
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!requestedType.Equals("all", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => x.Type == requestedType);
        if (!string.IsNullOrWhiteSpace(term)) query = query.Where(x => x.Subject.Contains(term) || x.Action.Contains(term) || x.Details.Contains(term));
        var audit = await query.Select(x => new RecordDto(x.Id, x.ResourceId, x.CreateAt, x.Type, x.Subject, x.Action, x.Details, x.AuditLogCode)).ToListAsync(cancellationToken);
        var selected = requestedType.Equals("all", StringComparison.OrdinalIgnoreCase) ? _providers.Values : _providers.TryGetValue(requestedType, out var provider) ? [provider] : [];
        var snapshots = new List<RecordDto>();
        foreach (var snapshotProvider in selected)
            snapshots.AddRange(await snapshotProvider.GetAsync(cancellationToken));
        var filteredSnapshots = string.IsNullOrWhiteSpace(term)
            ? snapshots
            : snapshots.Where(x => Matches(term, x.Subject, x.Action, x.Details));
        return audit.Concat(filteredSnapshots).OrderByDescending(x => x.Date).ToList();
    }

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
}
