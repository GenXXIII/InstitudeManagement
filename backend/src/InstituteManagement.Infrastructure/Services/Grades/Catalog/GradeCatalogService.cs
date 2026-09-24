using InstituteManagement.Application.Features.Grades;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Grades;

public sealed class GradeCatalogService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<GradeResponseDto>(db, cache), IGradeCatalogService
{
    public override CatalogResource Resource => CatalogResource.Grades;
    public override async Task<IReadOnlyList<GradeResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        var grades = await Db.GradeRecords.AsNoTracking().Include(grade => grade.Student).ThenInclude(student => student!.Department).Include(grade => grade.Course).Include(grade => grade.SubmittedByTeacher)
            .Where(grade => (grade.AcademicYear == period.AcademicYear && grade.Term == period.Term || !grade.FinalizedAtUtc.HasValue && grade.SubmittedByTeacherId.HasValue)
                && grade.Student!.Status != "Inactive"
                && grade.Course!.IsActive
                && (!departmentId.HasValue || grade.Student.DepartmentId == departmentId))
            .ToListAsync(ct);
        var sessions = await Db.ClassSessionRecords.AsNoTracking()
            .Where(session => session.AcademicYear == period.AcademicYear && session.Term == period.Term)
            .ToListAsync(ct);
        var sessionsByCourse = sessions.ToLookup(session => session.CourseId);
        return grades.Where(grade => Matches(search, grade.GradeCode, grade.Student!.FullName, grade.Course!.Name, grade.LetterGrade))
            .Select(grade => Response(grade, sessionsByCourse[grade.CourseId]))
            .ToList();
    }

    public override Task<GradeResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) =>
        throw new InvalidOperationException("Grades are generated automatically from students and cannot be added manually.");
    public override Task<GradeResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) =>
        throw new InvalidOperationException("Teacher grade submissions are read-only. Confirm or reject them in Assessment.");
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.GradeRecords.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) => Db.Remove(entity);
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct)
    {
        await Task.CompletedTask;
        throw new InvalidOperationException("Teacher grade submissions cannot be removed. Reject them in Assessment when correction is required.");
    }
    protected override GradeResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new GradeResponseDto(id, new GradeValuesDto(
            Get(values, "gradeCode"),
            Get(values, "studentId"),
            Get(values, "student"),
            Get(values, "courseId"),
            Get(values, "course"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "attendanceScore"),
            Get(values, "attendanceMaximum", "10"),
            Get(values, "attendancePresent", "0"),
            Get(values, "attendanceSessions", "0"),
            Get(values, "assignmentScore"),
            Get(values, "assignmentMaximum", "20"),
            Get(values, "midtermScore"),
            Get(values, "midtermMaximum", "20"),
            Get(values, "finalExamScore"),
            Get(values, "finalExamMaximum", "50"),
            Get(values, "score"),
            Get(values, "grade"),
            Get(values, "academicYear"),
            Get(values, "term", "Semester 1"),
            Get(values, "submittedByTeacherId"),
            Get(values, "submittedByTeacher"),
            Get(values, "reviewStatus", "Pending"),
            Get(values, "reviewNote"),
            Get(values, "submissionVersion", "1"),
            Get(values, "submittedAtUtc"),
            Get(values, "reviewedAtUtc"),
            Get(values, "finalizedAtUtc"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private async Task<GradeRecord> BuildAsync(GradeRecord entity, Dictionary<string, string> values, CancellationToken ct)
    {
        var gradeCode = Required(values, "gradeCode");
        await EnsureUniqueAsync(Db.GradeRecords.Where(item => item.Id != entity.Id && item.GradeCode == gradeCode), "GradeCode", ct);
        entity.GradeCode = gradeCode;
        entity.StudentId = await RelatedIdAsync<Student>(values, "studentId", ct);
        entity.CourseId = await RelatedIdAsync<Course>(values, "courseId", ct);
        var student = await Db.Students.FindAsync([entity.StudentId], ct);
        var course = await Db.Courses.FindAsync([entity.CourseId], ct);
        if (student is null || course is null || student.DepartmentId != course.DepartmentId || student.Status == "Inactive" || !course.IsActive)
            throw new InvalidOperationException("Student and course must be active and belong to the same department.");
        values["student"] = student.FullName;
        values["course"] = course.Name;
        values["departmentId"] = student.DepartmentId?.ToString() ?? "";
        values["department"] = student.Department?.Name ?? "";
        var rules = await GradeCompositionCalculator.LoadRulesAsync(Db, ct);
        var assignment = DecimalInRange(values, "assignmentScore", 0, rules.Weights.Assignment);
        var midterm = DecimalInRange(values, "midtermScore", 0, rules.Weights.Midterm);
        var finalExam = DecimalInRange(values, "finalExamScore", 0, rules.Weights.FinalExam);
        var attendance = await GradeCompositionCalculator.AttendanceAsync(Db, entity.StudentId, entity.CourseId, entity.AcademicYear, entity.Term, rules.Weights.Attendance, rules.Attendance, ct);
        GradeCompositionCalculator.Apply(entity, rules.Weights, rules.Thresholds, attendance, assignment, midterm, finalExam);
        values["attendanceScore"] = entity.AttendanceScore.ToString("0.##");
        values["attendanceMaximum"] = entity.AttendanceMaximum.ToString("0.##");
        values["attendancePresent"] = attendance.Attended.ToString();
        values["attendanceSessions"] = attendance.Sessions.ToString();
        values["assignmentMaximum"] = entity.AssignmentMaximum.ToString("0.##");
        values["midtermMaximum"] = entity.MidtermMaximum.ToString("0.##");
        values["finalExamMaximum"] = entity.FinalExamMaximum.ToString("0.##");
        values["score"] = entity.Score.ToString("0.##");
        values["grade"] = entity.LetterGrade;
        var period = await CurrentPeriodAsync(ct);
        entity.AcademicYear = period.AcademicYear;
        entity.Term = period.Term;
        values["academicYear"] = entity.AcademicYear;
        values["term"] = entity.Term;
        await EnsureUniqueAsync(
            Db.GradeRecords.Where(grade => grade.Id != entity.Id && grade.StudentId == entity.StudentId && grade.CourseId == entity.CourseId && grade.AcademicYear == entity.AcademicYear && grade.Term == entity.Term),
            "Grade for this student, course, academic year, and term",
            ct);
        var reminders = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "notifications" && x.Key == "gradeReminders").Select(x => x.Value).FirstOrDefaultAsync(ct);
        if (entity.LetterGrade is "E" or "F" && (!bool.TryParse(reminders, out var enabled) || enabled)) Db.Notifications.Add(new Notification { Title = "Grade support reminder", Message = $"{student?.FullName ?? "Student"} received {entity.LetterGrade} in {course?.Name ?? "a course"}.", Severity = entity.LetterGrade == "F" ? "Warning" : "Info" });
        return entity;
    }
    private static GradeResponseDto Response(GradeRecord grade, IEnumerable<ClassSessionRecord> sessions)
    {
        var attendance = GradeCompositionCalculator.Attendance(
            sessions,
            grade.StudentId,
            grade.AttendanceMaximum);
        return new GradeResponseDto(grade.Id, new GradeValuesDto(
            grade.GradeCode,
            grade.StudentId.ToString(),
            grade.Student?.FullName ?? "—",
            grade.CourseId.ToString(),
            grade.Course?.Name ?? "—",
            grade.Student?.DepartmentId.ToString() ?? "",
            grade.Student?.Department?.Name ?? "—",
            grade.AttendanceScore.ToString("0.##"),
            grade.AttendanceMaximum.ToString("0.##"),
            attendance.Attended.ToString(),
            attendance.Sessions.ToString(),
            grade.AssignmentScore.ToString("0.##"),
            grade.AssignmentMaximum.ToString("0.##"),
            grade.MidtermScore.ToString("0.##"),
            grade.MidtermMaximum.ToString("0.##"),
            grade.FinalExamScore.ToString("0.##"),
            grade.FinalExamMaximum.ToString("0.##"),
            grade.Score.ToString("0.##"),
            grade.LetterGrade,
            grade.AcademicYear,
            grade.Term,
            grade.SubmittedByTeacherId?.ToString() ?? "",
            grade.SubmittedByTeacher?.FullName ?? "—",
            grade.ReviewStatus,
            grade.ReviewNote,
            grade.SubmissionVersion.ToString(),
            grade.SubmittedAtUtc?.ToString("O") ?? "",
            grade.ReviewedAtUtc?.ToString("O") ?? "",
            grade.FinalizedAtUtc?.ToString("O") ?? "",
            grade.CreateAt.ToString("yyyy-MM-dd")));
    }
    private async Task<(string AcademicYear, string Term)> CurrentPeriodAsync(CancellationToken ct)
    {
        var settings = await Db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, ct);
        return (settings.GetValueOrDefault("academic-year:currentYear", "2026\u20132027"), settings.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }
}
