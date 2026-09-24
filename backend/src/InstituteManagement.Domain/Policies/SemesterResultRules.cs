namespace InstituteManagement.Domain.Policies;

public static class SemesterResultRules
{
    public const int ExpectedCourseCount = 5;

    public static decimal Average(IEnumerable<decimal> scores) =>
        Average(scores, ExpectedCourseCount);

    public static decimal Average(IEnumerable<decimal> scores, int expectedCourseCount) =>
        decimal.Round(scores.Sum() / Math.Max(1, expectedCourseCount), 2, MidpointRounding.AwayFromZero);

    public static string Outcome(
        int absentCount,
        IReadOnlyCollection<string> grades,
        decimal average,
        GradeThresholds thresholds,
        bool applyAttendanceRules)
        => Outcome(
            grades,
            average,
            thresholds,
            ExpectedCourseCount,
            applyAttendanceRules
                ? AttendanceResultRules.From(new Dictionary<string, string>()).Outcome(absentCount, 0)
                : null);

    public static string Outcome(
        IReadOnlyCollection<string> grades,
        decimal average,
        GradeThresholds thresholds,
        int expectedCourseCount,
        string? attendanceOutcome)
    {
        if (!string.IsNullOrWhiteSpace(attendanceOutcome)) return attendanceOutcome;
        if (grades.Any(grade => grade == "F")) return "Retake Exam";
        if (grades.Count < expectedCourseCount) return "Pending";
        return thresholds.Letter(average);
    }
}
