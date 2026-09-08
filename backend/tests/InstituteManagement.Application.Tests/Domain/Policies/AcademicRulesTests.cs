using InstituteManagement.Domain.Policies;

namespace InstituteManagement.Application.Tests.Domain.Policies;

public sealed class AcademicRulesTests
{
    private static readonly GradeThresholds Thresholds = GradeThresholds.From(
        new Dictionary<string, string>());

    [Theory]
    [InlineData(95, "A+")]
    [InlineData(90, "A")]
    [InlineData(85, "B+")]
    [InlineData(80, "B")]
    [InlineData(75, "C+")]
    [InlineData(70, "C")]
    [InlineData(60, "D")]
    [InlineData(59, "F")]
    public void Grade_thresholds_assign_expected_letter(decimal score, string expected) =>
        Assert.Equal(expected, Thresholds.Letter(score));

    [Fact]
    public void Semester_result_applies_attendance_failure_rule() =>
        Assert.Equal(
            "Fail",
            SemesterResultRules.Outcome(8, ["A", "A", "A", "A", "A"], 90, Thresholds, true));

    [Fact]
    public void Semester_result_stays_pending_until_all_courses_are_present() =>
        Assert.Equal(
            "Pending",
            SemesterResultRules.Outcome(0, ["A", "A", "A", "A"], 90, Thresholds, true));

    [Theory]
    [InlineData("Active", null, "Present")]
    [InlineData("Active", "Permission", "Permission")]
    [InlineData("Inactive", null, "Absent")]
    public void Teacher_presence_normalizes_domain_statuses(
        string? teacherStatus,
        string? assignmentStatus,
        string expected) =>
        Assert.Equal(expected, TeacherPresence.Attendance(teacherStatus, assignmentStatus));
}
