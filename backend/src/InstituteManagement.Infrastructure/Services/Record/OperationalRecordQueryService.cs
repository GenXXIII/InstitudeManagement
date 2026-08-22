using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class OperationalRecordQueryService(IEnumerable<IOperationalRecordReader> readers, InstituteDbContext db) : IOperationalRecordQueryService
{
    private readonly IReadOnlyDictionary<string, IOperationalRecordReader> _readers = readers.ToDictionary(x => x.Module, StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(string module, string? search, Guid? departmentId, bool history, CancellationToken cancellationToken)
    {
        if (!_readers.TryGetValue(module, out var reader)) throw new ArgumentException($"Operational records for '{module}' are not supported.");
        var records = await reader.GetAsync(departmentId, cancellationToken);
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm"))
            .ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
        var academicYear = settings.GetValueOrDefault("academic-year:currentYear");
        var term = settings.GetValueOrDefault("semester:currentTerm");
        records = FilterPeriod(records, academicYear, term, history);
        if (string.IsNullOrWhiteSpace(search)) return records;
        var searchTerm = search.Trim();
        return records.Where(x => Matches(searchTerm, x.Subject, x.Identifier, x.Status, x.Summary) || x.Activities.Any(a => a.Values.Any(v => v.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))).ToList();
    }

    private static IReadOnlyList<OperationalRecordDto> FilterPeriod(IReadOnlyList<OperationalRecordDto> records, string? academicYear, string? term, bool history)
    {
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(term)) return history ? [] : records.Where(x => x.Activities.Count > 0).ToList();
        return records.Select(record =>
        {
            var activities = record.Activities.Where(activity =>
            {
                var current = activity.GetValueOrDefault("Academic year") == academicYear && activity.GetValueOrDefault("Term") == term;
                return history ? !current : current;
            }).ToList();
            return activities.Count == 0 ? null : record with { Activities = activities, Summary = $"{activities.Count:N0} recorded activities" };
        }).OfType<OperationalRecordDto>().ToList();
    }

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
}
