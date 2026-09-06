using System.Text.Json;
using InstituteManagement.Application.Features.History;
using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
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
        var audit = await query.Select(x => new RecordDto(x.Id, x.ResourceId, x.CreateAt, x.Type, x.Subject, x.Action, x.Details, x.AuditLogCode)).ToListAsync(cancellationToken);
        var selected = requestedType.Equals("all", StringComparison.OrdinalIgnoreCase) ? _providers.Values : _providers.TryGetValue(requestedType, out var provider) ? [provider] : [];
        var snapshots = new List<RecordDto>();
        foreach (var snapshotProvider in selected)
            snapshots.AddRange(await snapshotProvider.GetAsync(cancellationToken));
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var rows = audit.Concat(snapshots).Select(item => item with { BusinessCode = HistoryCode(item, codeFormat) });
        if (!string.IsNullOrWhiteSpace(term)) rows = rows.Where(x => Matches(term, x.Subject, x.Action, x.Details, x.BusinessCode));
        return rows.OrderByDescending(x => x.Date).ToList();
    }

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

    private static string HistoryCode(RecordDto item, BusinessCodeFormatter.BusinessCodeFormat format)
    {
        var resource = Resource(item.Type);
        var source = SourceCode(item.Details, resource);
        return source is null ? "" : format.Derive(source, resource, "history");
    }

    private static string? SourceCode(string details, string resource)
    {
        try
        {
            using var document = JsonDocument.Parse(details);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            var expected = resource == "session" ? "classSessionRecordCode" : $"{resource}Code";
            foreach (var property in document.RootElement.EnumerateObject())
                if (property.Name.Equals(expected, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    private static string Resource(string type) => type.ToLowerInvariant() switch
    {
        "student" => "student",
        "teacher" => "teacher",
        "department" => "department",
        "course" => "course",
        "classroom" => "classroom",
        "timetable" => "timetable",
        "attendance" => "attendance",
        "grade" => "grade",
        "class session" or "session" => "session",
        _ => "session"
    };
}
