using System.Text.Json;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.History;
using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class HistoryQueryService(InstituteDbContext db, IEnumerable<IHistorySnapshotProvider> providers) : IHistoryQueryService
{
    private readonly IReadOnlyDictionary<string, IHistorySnapshotProvider> _providers = providers.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);

    public async Task<HistoryPageDto> GetAsync(string? search, string? type, PageRequest page, CancellationToken cancellationToken)
    {
        var requestedType = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim();
        var term = search?.Trim();
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!requestedType.Equals("all", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => x.Type == requestedType);
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.Subject.Contains(term) || x.Action.Contains(term) || x.Details.Contains(term) || x.AuditLogCode.Contains(term));
        var auditCount = await query.CountAsync(cancellationToken);
        var auditGroups = await query
            .GroupBy(x => new { x.Type, x.ResourceId, x.Subject })
            .Select(group => new
            {
                group.Key.Type,
                group.Key.ResourceId,
                group.Key.Subject,
                Latest = group.Max(x => x.CreateAt)
            })
            .ToListAsync(cancellationToken);
        var selected = requestedType.Equals("all", StringComparison.OrdinalIgnoreCase) ? _providers.Values : _providers.TryGetValue(requestedType, out var provider) ? [provider] : [];
        var snapshots = new List<RecordDto>();
        foreach (var snapshotProvider in selected)
            snapshots.AddRange(await snapshotProvider.GetAsync(cancellationToken));
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var snapshotRows = snapshots.Select(item => item with { BusinessCode = HistoryCode(item, codeFormat) });
        if (!string.IsNullOrWhiteSpace(term)) snapshotRows = snapshotRows.Where(x => Matches(term, x.Subject, x.Action, x.Details, x.BusinessCode));
        var materializedSnapshots = snapshotRows.ToList();

        var snapshotAliases = SnapshotAliases(materializedSnapshots);
        string AuditGroupKey(string auditType, Guid? resourceId, string subject) => resourceId.HasValue
            ? GroupKey(auditType, resourceId, subject)
            : snapshotAliases.GetValueOrDefault(Alias(auditType, subject)) ?? GroupKey(auditType, null, subject);

        var candidates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var snapshot in materializedSnapshots)
            AddCandidate(candidates, GroupKey(snapshot.Type, snapshot.ResourceId, snapshot.Subject), snapshot.Date);
        foreach (var group in auditGroups)
            AddCandidate(candidates, AuditGroupKey(group.Type, group.ResourceId, group.Subject), group.Latest);

        var selectedKeys = candidates
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(entry => entry.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedAuditGroups = auditGroups
            .Where(group => selectedKeys.Contains(AuditGroupKey(group.Type, group.ResourceId, group.Subject)))
            .ToList();
        var resourceIds = selectedAuditGroups.Where(group => group.ResourceId.HasValue).Select(group => group.ResourceId!.Value).Distinct().ToList();
        var orphanSubjects = selectedAuditGroups.Where(group => !group.ResourceId.HasValue).Select(group => group.Subject).Distinct().ToList();
        var audit = resourceIds.Count == 0 && orphanSubjects.Count == 0
            ? []
            : await query
                .Where(x => (x.ResourceId.HasValue && resourceIds.Contains(x.ResourceId.Value)) || (!x.ResourceId.HasValue && orphanSubjects.Contains(x.Subject)))
                .Select(x => new RecordDto(x.Id, x.ResourceId, x.CreateAt, x.Type, x.Subject, x.Action, x.Details, x.AuditLogCode))
                .ToListAsync(cancellationToken);
        var auditRows = audit
            .Where(item => selectedKeys.Contains(AuditGroupKey(item.Type, item.ResourceId, item.Subject)))
            .Select(item => item with { BusinessCode = HistoryCode(item, codeFormat) });
        var selectedSnapshots = materializedSnapshots.Where(item => selectedKeys.Contains(GroupKey(item.Type, item.ResourceId, item.Subject)));
        var items = auditRows.Concat(selectedSnapshots)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .ToList();
        return HistoryPageDto.Create(items, page, candidates.Count, auditCount + materializedSnapshots.Count);
    }

    private static void AddCandidate(IDictionary<string, DateTime> candidates, string key, DateTime date)
    {
        if (!candidates.TryGetValue(key, out var current) || date > current) candidates[key] = date;
    }

    private static Dictionary<string, string> SnapshotAliases(IEnumerable<RecordDto> snapshots)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var snapshot in snapshots.Where(item => item.ResourceId.HasValue))
        {
            var key = GroupKey(snapshot.Type, snapshot.ResourceId, snapshot.Subject);
            aliases[Alias(snapshot.Type, snapshot.Subject)] = key;
            try
            {
                using var document = JsonDocument.Parse(snapshot.Details);
                if (document.RootElement.ValueKind != JsonValueKind.Object) continue;
                foreach (var property in document.RootElement.EnumerateObject())
                    if (property.Value.ValueKind == JsonValueKind.String && IsIdentityAlias(property.Name))
                        aliases[Alias(snapshot.Type, property.Value.GetString() ?? string.Empty)] = key;
            }
            catch (JsonException) { }
        }
        return aliases;
    }

    private static bool IsIdentityAlias(string name) => name.Equals("name", StringComparison.OrdinalIgnoreCase)
        || name.Equals("fullName", StringComparison.OrdinalIgnoreCase)
        || name.Equals("studentCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("teacherCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("departmentCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("courseCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("classroomCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("timetableCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("attendanceCode", StringComparison.OrdinalIgnoreCase)
        || name.Equals("gradeCode", StringComparison.OrdinalIgnoreCase);

    private static string GroupKey(string type, Guid? resourceId, string subject) => resourceId.HasValue
        ? $"{type}:{resourceId.Value}"
        : $"{type}:subject:{subject.Trim().ToLowerInvariant()}";

    private static string Alias(string type, string value) => $"{type}:{value.Trim().ToLowerInvariant()}";

    private static bool Matches(string search, params string?[] values) => values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

    private static string HistoryCode(RecordDto item, BusinessCodeFormatter.BusinessCodeFormat format)
    {
        var resource = Resource(item.Type);
        var explicitHistory = JsonCode(item.Details, "historyCode");
        if (!string.IsNullOrWhiteSpace(explicitHistory)) return explicitHistory;
        var source = SourceCode(item.Details, resource);
        if (resource == "finance") return source ?? string.Empty;
        return source is null ? "" : format.Derive(source, resource, "history");
    }

    private static string? SourceCode(string details, string resource)
    {
        try
        {
            using var document = JsonDocument.Parse(details);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            var expected = resource == "session" ? "classSessionRecordCode" : resource == "finance" ? "financialAccountCode" : $"{resource}Code";
            foreach (var property in document.RootElement.EnumerateObject())
                if (property.Name.Equals(expected, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
        }
        catch (JsonException) { }
        return null;
    }

    private static string? JsonCode(string details, string key)
    {
        try
        {
            using var document = JsonDocument.Parse(details);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var property in document.RootElement.EnumerateObject())
                if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
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
        "finance" => "finance",
        "class session" or "session" => "session",
        _ => "session"
    };
}
