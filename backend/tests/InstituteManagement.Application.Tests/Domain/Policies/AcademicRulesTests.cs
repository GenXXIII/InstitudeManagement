using InstituteManagement.Domain.Policies;

namespace InstituteManagement.Application.Tests.Domain.Policies;

public sealed class AcademicRulesTests
{
    private static readonly GradeThresholds Thresholds = GradeThresholds.From(
        new Dictionary<string, string>());

    [Theory]
    [InlineData(90, "A")]
    [InlineData(89, "B")]
    [InlineData(80, "B")]
    [InlineData(79, "C")]
    [InlineData(70, "C")]
    [InlineData(60, "D")]
    [InlineData(50, "E")]
    [InlineData(49, "F")]
    public void Grade_thresholds_assign_expected_letter(decimal score, string expected) =>
        Assert.Equal(expected, Thresholds.Letter(score));

    [Fact]
    public void Semester_result_applies_attendance_failure_rule() =>
        Assert.Equal(
            "Fail",
            SemesterResultRules.Outcome(8, ["A", "A", "A", "A", "A"], 90, Thresholds, true));

    [Fact]
    public void Semester_result_requires_retake_from_six_absences() =>
        Assert.Equal(
            "Retake Exam",
            SemesterResultRules.Outcome(6, ["A", "A", "A", "A", "A"], 90, Thresholds, true));

    [Fact]
    public void Semester_result_requires_retake_when_any_course_is_f() =>
        Assert.Equal(
            "Retake Exam",
            SemesterResultRules.Outcome(0, ["A", "B", "C", "D", "F"], 70, Thresholds, true));

    [Fact]
    public void Semester_result_stays_pending_until_all_courses_are_present() =>
        Assert.Equal(
            "Pending",
            SemesterResultRules.Outcome(0, ["A", "A", "A", "A"], 90, Thresholds, true));

    [Fact]
    public void Grade_weights_default_to_requested_course_composition()
    {
        var weights = GradeWeights.From(new Dictionary<string, string>());

        Assert.Equal(10, weights.Attendance);
        Assert.Equal(20, weights.Assignment);
        Assert.Equal(20, weights.Midterm);
        Assert.Equal(50, weights.FinalExam);
        Assert.Equal(100, weights.Total);
        Assert.Equal(79, weights.Score(9, 18, 17, 35));
    }

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(1, 0, 8)]
    [InlineData(2, 0, 6)]
    [InlineData(5, 0, 0)]
    [InlineData(0, 12, 0)]
    public void Attendance_score_uses_configured_section_deductions(int absent, int permission, decimal expected)
    {
        var rules = AttendanceResultRules.From(new Dictionary<string, string>());

        Assert.Equal(expected, rules.Score(10, absent, permission));
    }

    [Theory]
    [InlineData(10, "A")]
    [InlineData(8.34, "A")]
    [InlineData(8.33, "B")]
    [InlineData(6.67, "B")]
    [InlineData(6.66, "C")]
    [InlineData(5, "C")]
    [InlineData(4.99, "D")]
    [InlineData(3.34, "D")]
    [InlineData(3.33, "E")]
    [InlineData(1.67, "E")]
    [InlineData(1.66, "F")]
    [InlineData(0, "F")]
    public void Attendance_grade_splits_the_configured_maximum_into_six_equal_bands(decimal score, string expected)
    {
        var rules = AttendanceResultRules.From(new Dictionary<string, string>());

        Assert.Equal(expected, rules.Grade(score, 10));
    }

    [Theory]
    [InlineData(5, 0, null)]
    [InlineData(6, 0, "Retake Exam")]
    [InlineData(8, 0, "Fail")]
    [InlineData(0, 12, "Retake Exam")]
    [InlineData(0, 14, "Fail")]
    public void Attendance_outcome_uses_configured_absent_and_permission_thresholds(int absent, int permission, string? expected)
    {
        var rules = AttendanceResultRules.From(new Dictionary<string, string>());

        Assert.Equal(expected, rules.Outcome(absent, permission));
    }

    [Theory]
    [InlineData("Active", null, "Present")]
    [InlineData("Active", "Permission", "Permission")]
    [InlineData("Inactive", null, "Absent")]
    public void Teacher_presence_normalizes_domain_statuses(
        string? teacherStatus,
        string? assignmentStatus,
        string expected) =>
        Assert.Equal(expected, TeacherPresence.Attendance(teacherStatus, assignmentStatus));

    [Theory]
    [InlineData(false, "Active", null, "Absent")]
    [InlineData(false, "Available", null, "Absent")]
    [InlineData(true, "Active", null, "Present")]
    [InlineData(true, "Available", null, "Present")]
    [InlineData(true, "Active", "Permission", "Permission")]
    [InlineData(true, "Inactive", null, "Absent")]
    public void Class_attendance_requires_an_explicit_class_start(
        bool classStarted,
        string? teacherStatus,
        string? assignmentStatus,
        string expected) =>
        Assert.Equal(expected, TeacherPresence.ClassAttendance(classStarted, teacherStatus, assignmentStatus));
}
