using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Common;

internal static class BusinessCodeFormatter
{
    private static readonly IReadOnlyDictionary<string, string[]> Prefixes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["student"] = ["STU", "ESTU", "OSTU", "RSTU", "HSTU"],
        ["teacher"] = ["TEA", "ETEA", "OTEA", "RTEA", "HTEA"],
        ["department"] = ["DEP", "EDEP", "ODEP", "RDEP", "HDEP"],
        ["course"] = ["COU", "ECOU", "OCOU", "RCOU", "HCOU"],
        ["classroom"] = ["CLA", "ECLA", "OCLA", "RCLA", "HCLA"],
        ["timetable"] = ["TIM", "ETIM", "OTIM", "RTIM", "HTIM"],
        ["attendance"] = ["ATT", "EATT", "OATT", "RATT", "HATT"],
        ["grade"] = ["GRD", "EGRD", "OGRD", "RGRD", "HGRD"],
        ["session"] = ["SES", "ESES", "OSES", "RSES", "HSES"]
    };

    private static readonly string[] Stages = ["management", "enrollment", "operation", "record", "history"];

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
        if (!Prefixes.TryGetValue(resource, out var fallbacks)) throw new ArgumentException("Code resource is invalid.");
        var settings = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "code-formats"
                || setting.Section == "academic-year" && setting.Key == "currentYear")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var stageIndex = Array.FindIndex(Stages, value => value.Equals(stage, StringComparison.OrdinalIgnoreCase));
        if (stageIndex < 0) throw new ArgumentException("Code stage is invalid.");
        var prefix = Value(settings, $"{resource}{Capitalize(stage)}Prefix", fallbacks[stageIndex]).ToUpperInvariant();
        var separator = Value(settings, "codeSeparator", "-");
        if (separator is not ("-" or "/" or "." or "_")) separator = "-";
        var configuredPrefixes = Stages.Select((value, index) => Value(settings, $"{resource}{Capitalize(value)}Prefix", fallbacks[index])).ToList();
        var suffix = StripPrefix(raw.ToUpperInvariant(), configuredPrefixes);
        if (string.IsNullOrWhiteSpace(suffix)) throw new ArgumentException($"{DisplayName(key)} must include a value after its prefix.");
        var includeYear = bool.TryParse(Value(settings, "codeIncludeYear", "false"), out var enabled) && enabled;
        var year = Value(settings, "currentYear", DateTime.UtcNow.Year.ToString()).Split('–', '-', '/')[0];
        if (includeYear && suffix.StartsWith($"{year}{separator}", StringComparison.OrdinalIgnoreCase))
            suffix = suffix[(year.Length + separator.Length)..];
        var padding = int.TryParse(Value(settings, "codePaddingWidth", "1"), out var width) ? Math.Clamp(width, 1, 12) : 1;
        if (suffix.All(char.IsDigit)) suffix = suffix.PadLeft(padding, '0');
        var result = string.Join(separator, new[] { prefix }.Concat(includeYear ? [year, suffix] : [suffix])).ToUpperInvariant();
        if (result.Length > 64 || result.Any(character => !char.IsLetterOrDigit(character) && character is not ('.' or '_' or '/' or '-')))
            throw new ArgumentException($"{DisplayName(key)} must be 1 to 64 characters using letters, numbers, dot, underscore, slash, or hyphen.");
        return result;
    }

    private static string StripPrefix(string value, IEnumerable<string> prefixes)
    {
        foreach (var prefix in prefixes.Where(value => !string.IsNullOrWhiteSpace(value)).OrderByDescending(value => value.Length))
        {
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var remainder = value[prefix.Length..];
            if (remainder.Length == 0) return "";
            if (remainder[0] is '.' or '_' or '/' or '-') return remainder[1..];
            if (char.IsDigit(remainder[0])) return remainder;
        }
        return value;
    }

    private static string Value(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
        values.GetValueOrDefault($"code-formats:{key}")
        ?? values.GetValueOrDefault($"academic-year:{key}")
        ?? fallback;

    private static string Capitalize(string value) => $"{char.ToUpperInvariant(value[0])}{value[1..]}";

    private static string DisplayName(string key) => key == "enrollmentCode" ? "EnrollmentCode" : $"{char.ToUpperInvariant(key[0])}{key[1..]}";
}
