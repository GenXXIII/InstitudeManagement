using InstituteManagement.Infrastructure.Services.Grades;

namespace InstituteManagement.Infrastructure.Services.Results;

internal static class SemesterResultRules
{
    public const int ExpectedCourseCount = 5;

    public static decimal Average(IEnumerable<decimal> scores) =>
        decimal.Round(scores.Sum() / ExpectedCourseCount, 2);

    public static string Outcome(int absentCount, IReadOnlyCollection<string> grades, decimal average, GradeThresholds thresholds, bool applyAttendanceRules)
    {
        if (applyAttendanceRules && absentCount >= 8) return "Fail";
        if (applyAttendanceRules && absentCount >= 6) return "Retake Exam";
        if (grades.Any(grade => grade == "F")) return "Retake Exam";
        if (grades.Count < ExpectedCourseCount) return "Pending";
        return thresholds.Letter(average);
    }
}
