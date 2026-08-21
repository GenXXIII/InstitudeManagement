using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class OperationalRecordQueryService(IEnumerable<IOperationalRecordReader> readers) : IOperationalRecordQueryService
{
    private readonly IReadOnlyDictionary<string, IOperationalRecordReader> _readers = readers.ToDictionary(x => x.Module, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(string module, string? search, Guid? departmentId, CancellationToken cancellationToken)
    {
        if (!_readers.TryGetValue(module, out var reader)) throw new ArgumentException($"Operational records for '{module}' are not supported.");
        var records = await reader.GetAsync(departmentId, cancellationToken);
        if (string.IsNullOrWhiteSpace(search)) return records;
        var term = search.Trim();
        return records.Where(x => Matches(term, x.Subject, x.Identifier, x.Status, x.Summary) || x.Activities.Any(a => a.Values.Any(v => v.Contains(term, StringComparison.OrdinalIgnoreCase)))).ToList();
    }

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
}
