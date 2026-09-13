using System.Globalization;

namespace InstituteManagement.Domain.Policies;

public sealed record GradeWeights(decimal Attendance, decimal Assignment, decimal Midterm, decimal FinalExam)
{
    public decimal Total => Attendance + Assignment + Midterm + FinalExam;

    public static GradeWeights From(IReadOnlyDictionary<string, string> values)
    {
        decimal Weight(string key, decimal fallback) =>
            decimal.TryParse(values.GetValueOrDefault(key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        return new GradeWeights(
            Weight("attendanceWeight", 10),
            Weight("assignmentWeight", 20),
            Weight("midtermWeight", 20),
            Weight("finalExamWeight", 50));
    }

    public decimal Score(decimal attendance, decimal assignment, decimal midterm, decimal finalExam) =>
        decimal.Round(attendance + assignment + midterm + finalExam, 2, MidpointRounding.AwayFromZero);
}
