using System.Globalization;

namespace InstituteManagement.Domain.Policies;

public sealed record AttendanceResultRules(
    decimal AbsentDeduction,
    decimal PermissionDeduction,
    int RetakeAbsentSections,
    int FailAbsentSections,
    int RetakePermissionSections,
    int FailPermissionSections)
{
    public static AttendanceResultRules From(IReadOnlyDictionary<string, string> values)
    {
        decimal DecimalRule(string key, decimal fallback) =>
            decimal.TryParse(values.GetValueOrDefault(key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        int IntegerRule(string key, int fallback) =>
            int.TryParse(values.GetValueOrDefault(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        return new AttendanceResultRules(
            DecimalRule("absentScoreDeduction", 2m),
            DecimalRule("permissionScoreDeduction", 0.91m),
            IntegerRule("retakeAbsentSections", 6),
            IntegerRule("failAbsentSections", 8),
            IntegerRule("retakePermissionSections", 12),
            IntegerRule("failPermissionSections", 14));
    }

    public decimal Score(decimal maximum, int absentSections, int permissionSections) =>
        decimal.Round(
            Math.Clamp(maximum - absentSections * AbsentDeduction - permissionSections * PermissionDeduction, 0m, maximum),
            2,
            MidpointRounding.AwayFromZero);

    public string Grade(decimal score, decimal maximum)
    {
        if (maximum <= 0) return "F";
        var bounded = Math.Clamp(score, 0m, maximum);
        return bounded >= maximum * 5m / 6m ? "A" :
            bounded >= maximum * 4m / 6m ? "B" :
            bounded >= maximum * 3m / 6m ? "C" :
            bounded >= maximum * 2m / 6m ? "D" :
            bounded >= maximum * 1m / 6m ? "E" : "F";
    }

    public string? Outcome(int absentSections, int permissionSections)
    {
        if (absentSections >= FailAbsentSections || permissionSections >= FailPermissionSections) return "Fail";
        if (absentSections >= RetakeAbsentSections || permissionSections >= RetakePermissionSections) return "Retake Exam";
        return null;
    }
}
