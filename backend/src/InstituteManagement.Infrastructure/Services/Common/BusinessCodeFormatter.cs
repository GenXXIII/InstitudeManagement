using System.Text.RegularExpressions;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Common;

internal static partial class BusinessCodeFormatter
{
    private static readonly string[] Stages = ["management", "enrollment", "operation", "record", "history"];
    private static readonly IReadOnlyDictionary<string, string[]> Prefixes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["student"] = ["STU", "ESTU", "OPE", "REC", "HIS"],
        ["teacher"] = ["TEA", "ETEA", "OPE", "REC", "HIS"],
        ["department"] = ["DEP", "EDEP", "OPE", "REC", "HIS"],
        ["course"] = ["COU", "ECOU", "OPE", "REC", "HIS"],
        ["classroom"] = ["CLA", "ECLA", "OPE", "REC", "HIS"],
        ["timetable"] = ["TIM", "ETIM", "OPE", "REC", "HIS"],
        ["attendance"] = ["ATT", "EATT", "OPE", "REC", "HIS"],
        ["grade"] = ["GRD", "EGRD", "OPE", "REC", "HIS"],
        ["session"] = ["SES", "ESES", "OPE", "REC", "HIS"]
    };

    public static async Task<string> GenerateAsync(InstituteDbContext db, string resource, CancellationToken cancellationToken)
        => (await GenerateManyAsync(db, resource, 1, cancellationToken))[0];

    public static async Task<IReadOnlyList<string>> GenerateManyAsync(InstituteDbContext db, string resource, int count, CancellationToken cancellationToken)
    {
        if (count < 1) return [];
        var format = await LoadAsync(db, cancellationToken);
        var codes = await CodesAsync(db, resource, cancellationToken);
        var greatestAssigned = codes.Concat(TrackedCodes(db, resource))
            .Select(NumericSuffix)
            .DefaultIfEmpty(format.StartingNumber - 1)
            .Max();
        var first = Math.Max(format.StartingNumber, greatestAssigned + 1);
        return Enumerable.Range(0, count).Select(offset => format.Management(resource, first + offset)).ToList();
    }

    public static async Task<string> DeriveAsync(
        InstituteDbContext db,
        string sourceCode,
        string resource,
        string stage,
        CancellationToken cancellationToken) =>
        (await LoadAsync(db, cancellationToken)).Derive(sourceCode, resource, stage);

    public static async Task<string> GenerateEnrollmentAsync(
        InstituteDbContext db,
        string managementCode,
        string resource,
        Guid resourceId,
        CancellationToken cancellationToken)
        => (await GenerateEnrollmentWorkflowAsync(db, managementCode, resource, resourceId, cancellationToken)).Enrollment;

    public static async Task<WorkflowCodeChain> GenerateEnrollmentWorkflowAsync(
        InstituteDbContext db,
        string managementCode,
        string resource,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var persisted = resource.ToLowerInvariant() switch
        {
            "student" => await db.StudentEnrollments.CountAsync(item => item.StudentId == resourceId, cancellationToken),
            "teacher" => await db.TeacherAssignments.CountAsync(item => item.TeacherId == resourceId, cancellationToken),
            "course" => await db.CourseAssignments.CountAsync(item => item.CourseId == resourceId, cancellationToken),
            "classroom" => await db.ClassroomAssignments.CountAsync(item => item.ClassroomId == resourceId, cancellationToken),
            "timetable" => await db.TimetableEnrollments.CountAsync(item => item.ScheduleEntryId == resourceId, cancellationToken),
            _ => throw new ArgumentException("Enrollment codes are not supported for this resource.", nameof(resource))
        };
        var pending = resource.ToLowerInvariant() switch
        {
            "student" => db.StudentEnrollments.Local.Count(item => item.StudentId == resourceId && db.Entry(item).State == EntityState.Added),
            "teacher" => db.TeacherAssignments.Local.Count(item => item.TeacherId == resourceId && db.Entry(item).State == EntityState.Added),
            "course" => db.CourseAssignments.Local.Count(item => item.CourseId == resourceId && db.Entry(item).State == EntityState.Added),
            "classroom" => db.ClassroomAssignments.Local.Count(item => item.ClassroomId == resourceId && db.Entry(item).State == EntityState.Added),
            "timetable" => db.TimetableEnrollments.Local.Count(item => item.ScheduleEntryId == resourceId && db.Entry(item).State == EntityState.Added),
            _ => 0
        };
        var format = await LoadAsync(db, cancellationToken);
        var occurrence = persisted + pending + 1L;
        return new WorkflowCodeChain(
            format.Derive(managementCode, resource, "management"),
            format.Linked(managementCode, resource, "enrollment", occurrence),
            format.Linked(managementCode, resource, "operation", occurrence),
            format.Linked(managementCode, resource, "record", occurrence),
            format.Linked(managementCode, resource, "history", occurrence));
    }

    public static async Task<string> GenerateEnrollmentScopedAsync(
        InstituteDbContext db,
        string managementCode,
        string resource,
        string enrollmentCode,
        string prefixKey,
        string fallbackPrefix,
        CancellationToken cancellationToken) =>
        (await LoadAsync(db, cancellationToken)).LinkedWithConfiguredPrefix(
            managementCode,
            resource,
            enrollmentCode,
            prefixKey,
            fallbackPrefix);

    public static async Task<string> GenerateEnrollmentPublicIdAsync(
        InstituteDbContext db,
        Guid enrollmentId,
        string prefixKey,
        string fallbackPrefix,
        CancellationToken cancellationToken) =>
        (await LoadAsync(db, cancellationToken)).EnrollmentPublicId(enrollmentId, prefixKey, fallbackPrefix);

    public static async Task<string> FormatAsync(
        InstituteDbContext db,
        IReadOnlyDictionary<string, string> values,
        string key,
        string resource,
        string stage,
        CancellationToken cancellationToken)
    {
        var raw = values.GetValueOrDefault(key)?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException($"{DisplayName(key)} is required.");
        return (await LoadAsync(db, cancellationToken)).Format(raw, resource, stage, DisplayName(key));
    }

    public static async Task<string> FormatStandaloneAsync(
        InstituteDbContext db,
        string? raw,
        string prefixKey,
        string fallbackPrefix,
        string label,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new ArgumentException($"{label} is required.");
        return (await LoadAsync(db, cancellationToken)).FormatStandalone(raw.Trim(), prefixKey, fallbackPrefix, label);
    }

    public static async Task<bool> HasAssignedSequenceAsync(IQueryable<string> assignedCodes, string candidate, CancellationToken cancellationToken)
    {
        var candidateSequence = NumericSuffix(candidate);
        return (await assignedCodes.ToListAsync(cancellationToken)).Any(existing =>
            candidateSequence >= 0
                ? NumericSuffix(existing) == candidateSequence
                : existing.Equals(candidate, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<string> RecommendAvailableAsync(
        InstituteDbContext db,
        IQueryable<string> assignedCodes,
        string candidate,
        CancellationToken cancellationToken)
    {
        var format = await LoadAsync(db, cancellationToken);
        var used = (await assignedCodes.ToListAsync(cancellationToken))
            .Select(NumericSuffix)
            .Where(sequence => sequence >= format.StartingNumber)
            .ToHashSet();
        var recommendation = format.StartingNumber;
        while (used.Contains(recommendation))
        {
            if (recommendation == long.MaxValue) throw new InvalidOperationException("No code sequence is available.");
            recommendation++;
        }
        return format.WithSequence(candidate, recommendation);
    }

    public static async Task<BusinessCodeFormat> LoadAsync(InstituteDbContext db, CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "code-formats"
                || setting.Section == "academic-year" && setting.Key == "currentYear")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var separator = Value(settings, "codeSeparator", "-");
        if (separator is not ("-" or "/" or "." or "_")) separator = "-";
        var includeYear = bool.TryParse(Value(settings, "codeIncludeYear", "false"), out var enabled) && enabled;
        var year = YearPattern().Match(Value(settings, "currentYear", DateTime.UtcNow.Year.ToString())).Value;
        if (string.IsNullOrWhiteSpace(year)) year = DateTime.UtcNow.Year.ToString();
        var padding = int.TryParse(Value(settings, "codePaddingWidth", "1"), out var width) ? Math.Clamp(width, 1, 12) : 1;
        var startingNumber = long.TryParse(Value(settings, "codeStartingNumber", "1"), out var start) ? Math.Max(0, start) : 1;
        return new BusinessCodeFormat(settings, separator, includeYear, year, padding, startingNumber);
    }

    private static async Task<IReadOnlyList<string>> CodesAsync(InstituteDbContext db, string resource, CancellationToken cancellationToken) => resource.ToLowerInvariant() switch
    {
        "student" => await db.Students.AsNoTracking().Select(item => item.StudentCode).ToListAsync(cancellationToken),
        "teacher" => await db.Teachers.AsNoTracking().Select(item => item.TeacherCode).ToListAsync(cancellationToken),
        "department" => await db.Departments.AsNoTracking().Select(item => item.DepartmentCode).ToListAsync(cancellationToken),
        "course" => await db.Courses.AsNoTracking().Select(item => item.CourseCode).ToListAsync(cancellationToken),
        "classroom" => await db.Classrooms.AsNoTracking().Select(item => item.ClassroomCode).ToListAsync(cancellationToken),
        "timetable" => await db.ScheduleEntries.AsNoTracking().Select(item => item.TimetableCode).ToListAsync(cancellationToken),
        "attendance" => await db.AttendanceRecords.AsNoTracking().Select(item => item.AttendanceCode).ToListAsync(cancellationToken),
        "grade" => await db.GradeRecords.AsNoTracking().Select(item => item.GradeCode).ToListAsync(cancellationToken),
        "session" => await db.ClassSessionRecords.AsNoTracking().Select(item => item.ClassSessionRecordCode).ToListAsync(cancellationToken),
        _ => throw new ArgumentException("Code resource is invalid.")
    };

    private static IEnumerable<string> TrackedCodes(InstituteDbContext db, string resource) => resource.ToLowerInvariant() switch
    {
        "student" => db.Students.Local.Select(item => item.StudentCode),
        "teacher" => db.Teachers.Local.Select(item => item.TeacherCode),
        "department" => db.Departments.Local.Select(item => item.DepartmentCode),
        "course" => db.Courses.Local.Select(item => item.CourseCode),
        "classroom" => db.Classrooms.Local.Select(item => item.ClassroomCode),
        "timetable" => db.ScheduleEntries.Local.Select(item => item.TimetableCode),
        "attendance" => db.AttendanceRecords.Local.Select(item => item.AttendanceCode),
        "grade" => db.GradeRecords.Local.Select(item => item.GradeCode),
        "session" => db.ClassSessionRecords.Local.Select(item => item.ClassSessionRecordCode),
        _ => []
    };

    internal sealed class BusinessCodeFormat(
        IReadOnlyDictionary<string, string> values,
        string separator,
        bool includeYear,
        string year,
        int padding,
        long startingNumber)
    {
        public long StartingNumber { get; } = startingNumber;

        public string Management(string resource, long sequence)
        {
            var prefix = Prefix(resource, "management");
            var number = sequence.ToString().PadLeft(padding, '0');
            return Validate(string.Join(separator, new[] { prefix }.Concat(includeYear ? [year, number] : [number])));
        }

        public string Derive(string sourceCode, string resource, string stage)
        {
            var source = ManagementSource(sourceCode, resource);
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source management code is required.");
            if (stage.Equals("management", StringComparison.OrdinalIgnoreCase)) return Validate(source);
            var occurrence = WorkflowOccurrence(sourceCode, resource);
            return Linked(source, resource, stage, occurrence < 1 ? 1 : occurrence);
        }

        public WorkflowCodeChain Chain(string managementCode, string? enrollmentCode, string resource)
        {
            var management = Derive(managementCode, resource, "management");
            var occurrence = string.IsNullOrWhiteSpace(enrollmentCode) ? 1 : WorkflowOccurrence(enrollmentCode, resource);
            if (occurrence < 1) occurrence = 1;
            var enrollment = Linked(management, resource, "enrollment", occurrence);
            var operation = Linked(management, resource, "operation", occurrence);
            var record = Linked(management, resource, "record", occurrence);
            return new WorkflowCodeChain(
                management,
                enrollment,
                operation,
                record,
                Linked(management, resource, "history", occurrence));
        }

        public WorkflowCodeChain Chain(
            string managementCode,
            string enrollmentCode,
            string operationCode,
            string recordCode,
            string historyCode,
            string resource)
        {
            var fallback = Chain(managementCode, enrollmentCode, resource);
            return new WorkflowCodeChain(
                managementCode,
                string.IsNullOrWhiteSpace(enrollmentCode) ? fallback.Enrollment : enrollmentCode,
                string.IsNullOrWhiteSpace(operationCode) ? fallback.Operation : operationCode,
                string.IsNullOrWhiteSpace(recordCode) ? fallback.Record : recordCode,
                string.IsNullOrWhiteSpace(historyCode) ? fallback.History : historyCode);
        }

        public string Linked(string managementCode, string resource, string stage, long occurrence)
        {
            if (occurrence < 1) throw new ArgumentOutOfRangeException(nameof(occurrence));
            var management = ManagementSource(managementCode, resource);
            if (string.IsNullOrWhiteSpace(management)) throw new ArgumentException("Source management code is required.");
            var number = occurrence.ToString().PadLeft(padding, '0');
            return Validate(string.Join(separator, Prefix(resource, stage), number, management));
        }

        public string LinkedWithConfiguredPrefix(
            string managementCode,
            string resource,
            string enrollmentCode,
            string prefixKey,
            string fallbackPrefix)
        {
            var occurrence = WorkflowOccurrence(enrollmentCode, resource);
            if (occurrence < 1) occurrence = 1;
            return LinkedWithConfiguredPrefix(managementCode, resource, occurrence, prefixKey, fallbackPrefix);
        }

        public string LinkedWithConfiguredPrefix(
            string managementCode,
            string resource,
            long occurrence,
            string prefixKey,
            string fallbackPrefix)
        {
            if (occurrence < 1) throw new ArgumentOutOfRangeException(nameof(occurrence));
            var management = ManagementSource(managementCode, resource);
            var number = occurrence.ToString().PadLeft(padding, '0');
            var prefix = Value(values, prefixKey, fallbackPrefix).Trim().ToUpperInvariant();
            return Validate(string.Join(separator, prefix, number, management));
        }

        public string EnrollmentPublicId(Guid enrollmentId, string prefixKey, string fallbackPrefix)
        {
            var prefix = Value(values, prefixKey, fallbackPrefix).Trim().ToUpperInvariant();
            return PublicAccessId.ForEnrollment(prefix, enrollmentId);
        }

        public string Format(string raw, string resource, string stage, string label)
        {
            var prefixes = Stages.Select(configuredStage => Prefix(resource, configuredStage));
            var suffix = StripPrefix(raw.Trim().ToUpperInvariant(), prefixes);
            return Build(Prefix(resource, stage), suffix, label);
        }

        public string FormatStandalone(string raw, string prefixKey, string fallbackPrefix, string label)
        {
            var prefix = Value(values, prefixKey, fallbackPrefix).Trim().ToUpperInvariant();
            return Build(prefix, StripPrefix(raw.Trim().ToUpperInvariant(), [prefix]), label);
        }

        public string WithSequence(string formattedCode, long sequence)
        {
            var result = formattedCode.Trim().ToUpperInvariant();
            var number = sequence.ToString().PadLeft(padding, '0');
            var suffix = NumericSuffixPattern().Match(result);
            if (suffix.Success) return Validate($"{result[..suffix.Index]}{number}");
            var separatorIndex = result.LastIndexOfAny(['-', '/', '.', '_']);
            if (separatorIndex < 0) throw new ArgumentException("Formatted code must include a prefix and separator.");
            return Validate($"{result[..(separatorIndex + 1)]}{number}");
        }

        private string Build(string prefix, string suffix, string label)
        {
            suffix = suffix.Trim('.', '_', '/', '-');
            if (includeYear && suffix.StartsWith($"{year}{separator}", StringComparison.OrdinalIgnoreCase))
                suffix = suffix[(year.Length + separator.Length)..];
            if (string.IsNullOrWhiteSpace(suffix)) throw new ArgumentException($"{label} must include a sequence after its prefix.");
            if (suffix.All(char.IsDigit)) suffix = suffix.PadLeft(padding, '0');
            return Validate(string.Join(separator, new[] { prefix }.Concat(includeYear ? [year, suffix] : [suffix])));
        }

        private string ManagementSource(string sourceCode, string resource)
        {
            var source = sourceCode.Trim().TrimEnd('.', '_', '/', '-').ToUpperInvariant();
            foreach (var stage in Stages.Skip(1))
            {
                var prefix = Regex.Escape(Prefix(resource, stage));
                var separatorPattern = Regex.Escape(separator);
                var current = Regex.Match(source, $"^{prefix}{separatorPattern}\\d+{separatorPattern}(?<management>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (current.Success) return current.Groups["management"].Value;
                source = Regex.Replace(source, $"{separatorPattern}{prefix}{separatorPattern}\\d+$", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return source;
        }

        private long WorkflowOccurrence(string sourceCode, string resource)
        {
            var source = sourceCode.Trim().TrimEnd('.', '_', '/', '-').ToUpperInvariant();
            var generic = Regex.Match(source, $"^[^{Regex.Escape(separator)}]+{Regex.Escape(separator)}(?<occurrence>\\d+){Regex.Escape(separator)}.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (generic.Success && long.TryParse(generic.Groups["occurrence"].Value, out var genericOccurrence)) return genericOccurrence;
            foreach (var stage in Stages.Skip(1))
            {
                var prefix = Regex.Escape(Prefix(resource, stage));
                var separatorPattern = Regex.Escape(separator);
                var current = Regex.Match(source, $"^{prefix}{separatorPattern}(?<occurrence>\\d+){separatorPattern}.+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (current.Success && long.TryParse(current.Groups["occurrence"].Value, out var occurrence)) return occurrence;
                var legacy = Regex.Match(source, $"{separatorPattern}{prefix}{separatorPattern}(?<occurrence>\\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (legacy.Success && long.TryParse(legacy.Groups["occurrence"].Value, out occurrence)) return occurrence;
            }
            return -1;
        }

        private string Prefix(string resource, string stage)
        {
            if (!Prefixes.TryGetValue(resource, out var fallbacks)) throw new ArgumentException("Code resource is invalid.");
            var stageIndex = Array.FindIndex(Stages, value => value.Equals(stage, StringComparison.OrdinalIgnoreCase));
            if (stageIndex < 0) throw new ArgumentException("Code stage is invalid.");
            var modernKey = $"{resource}{Capitalize(stage)}Prefix";
            var singlePrefixFallback = stageIndex == 0 ? Value(values, $"{resource}Prefix", fallbacks[stageIndex]) : fallbacks[stageIndex];
            return Value(values, modernKey, singlePrefixFallback).Trim().ToUpperInvariant();
        }

        private static string StripPrefix(string value, IEnumerable<string> prefixes)
        {
            foreach (var prefix in prefixes.Where(candidate => !string.IsNullOrWhiteSpace(candidate)).OrderByDescending(candidate => candidate.Length))
            {
                if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                var remainder = value[prefix.Length..];
                if (remainder.Length == 0) return "";
                if (remainder[0] is '.' or '_' or '/' or '-') return remainder[1..];
                if (char.IsDigit(remainder[0])) return remainder;
            }
            return value;
        }

        private static string Validate(string result)
        {
            result = result.ToUpperInvariant();
            if (result.Length > 64 || result.Any(character => !char.IsLetterOrDigit(character) && character is not ('.' or '_' or '/' or '-')))
                throw new ArgumentException("Generated code must be 1 to 64 characters using letters, numbers, dot, underscore, slash, or hyphen.");
            return result;
        }
    }

    internal sealed record WorkflowCodeChain(
        string Management,
        string Enrollment,
        string Operation,
        string Record,
        string History);

    private static long NumericSuffix(string value)
    {
        var match = NumericSuffixPattern().Match(value.Trim());
        return match.Success && long.TryParse(match.Value, out var number) ? number : -1;
    }

    private static string Value(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
        values.GetValueOrDefault($"code-formats:{key}")
        ?? values.GetValueOrDefault($"academic-year:{key}")
        ?? fallback;

    private static string Capitalize(string value) => $"{char.ToUpperInvariant(value[0])}{value[1..]}";

    private static string DisplayName(string key) => key == "enrollmentCode" ? "EnrollmentCode" : $"{char.ToUpperInvariant(key[0])}{key[1..]}";

    [GeneratedRegex(@"\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex NumericSuffixPattern();

    [GeneratedRegex(@"\d{4}", RegexOptions.CultureInvariant)]
    private static partial Regex YearPattern();
}
