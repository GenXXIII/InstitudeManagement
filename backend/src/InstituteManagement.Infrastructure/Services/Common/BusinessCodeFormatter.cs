using System.Text.RegularExpressions;
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
        ["session"] = ["SES", "ESES", "OPE", "REC", "HIS"],
        ["alert"] = ["ALT", "EALT", "OPE", "REC", "HIS"]
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
        "alert" => await db.Announcements.AsNoTracking().Select(item => item.AnnouncementCode).ToListAsync(cancellationToken),
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
        "alert" => db.Announcements.Local.Select(item => item.AnnouncementCode),
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
            var sequence = NumericSuffix(source);
            if (sequence < 0) throw new ArgumentException("Source management code must end with a number.");
            var number = sequence.ToString().PadLeft(padding, '0');
            return Validate(string.Join(separator, source, Prefix(resource, stage), number));
        }

        private string ManagementSource(string sourceCode, string resource)
        {
            var source = sourceCode.Trim().ToUpperInvariant();
            foreach (var stage in Stages.Skip(1))
            {
                var pattern = $"{Regex.Escape(separator)}{Regex.Escape(Prefix(resource, stage))}{Regex.Escape(separator)}\\d+$";
                source = Regex.Replace(source, pattern, "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return source;
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

        private static string Validate(string result)
        {
            result = result.ToUpperInvariant();
            if (result.Length > 64 || result.Any(character => !char.IsLetterOrDigit(character) && character is not ('.' or '_' or '/' or '-')))
                throw new ArgumentException("Generated code must be 1 to 64 characters using letters, numbers, dot, underscore, slash, or hyphen.");
            return result;
        }
    }

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

    [GeneratedRegex(@"\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex NumericSuffixPattern();

    [GeneratedRegex(@"\d{4}", RegexOptions.CultureInvariant)]
    private static partial Regex YearPattern();
}
