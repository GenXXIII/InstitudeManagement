namespace InstituteManagement.Infrastructure.Services.Grades;

internal sealed record GradeThresholds(decimal A, decimal B, decimal C, decimal D, decimal E)
{
    public static GradeThresholds From(IReadOnlyDictionary<string, string> values)
    {
        decimal Rule(string key, decimal fallback) => decimal.TryParse(values.GetValueOrDefault(key), out var value) ? value : fallback;
        return new GradeThresholds(Rule("aMinimum", 90), Rule("bMinimum", 80), Rule("cMinimum", 70), Rule("dMinimum", 60), Rule("eMinimum", 50));
    }

    public string Letter(decimal score) => score >= A ? "A" : score >= B ? "B" : score >= C ? "C" : score >= D ? "D" : score >= E ? "E" : "F";
}
