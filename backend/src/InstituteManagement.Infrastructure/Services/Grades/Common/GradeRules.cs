using System.Globalization;

namespace InstituteManagement.Infrastructure.Services.Grades;

internal sealed record GradeThresholds(
    decimal APlus,
    decimal A,
    decimal BPlus,
    decimal B,
    decimal CPlus,
    decimal C,
    decimal D)
{
    public static GradeThresholds From(IReadOnlyDictionary<string, string> values)
    {
        decimal Rule(string key, decimal fallback) =>
            decimal.TryParse(values.GetValueOrDefault(key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        var a = Rule("aMinimum", 90);
        var b = Rule("bMinimum", 80);
        var c = Rule("cMinimum", 70);
        return new GradeThresholds(
            Rule("aPlusMinimum", (100 + a) / 2),
            a,
            Rule("bPlusMinimum", (a + b) / 2),
            b,
            Rule("cPlusMinimum", (b + c) / 2),
            c,
            Rule("dMinimum", 60));
    }

    public string Letter(decimal score) =>
        score >= APlus ? "A+" :
        score >= A ? "A" :
        score >= BPlus ? "B+" :
        score >= B ? "B" :
        score >= CPlus ? "C+" :
        score >= C ? "C" :
        score >= D ? "D" : "F";
}
