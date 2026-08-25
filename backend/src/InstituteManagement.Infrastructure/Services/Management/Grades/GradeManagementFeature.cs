using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Grades;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Grades;

public sealed class GradeManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "grades";
    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        var grades = await Db.GradeRecords.AsNoTracking().Include(grade => grade.Student).ThenInclude(student => student!.Department).Include(grade => grade.Course)
            .Where(grade => grade.AcademicYear == period.AcademicYear && grade.Term == period.Term && grade.Student!.Status != "Inactive" && grade.Course!.IsActive && (!departmentId.HasValue || grade.Student.DepartmentId == departmentId))
            .ToListAsync(ct);
        return grades.Where(grade => Matches(search, grade.GradeCode, grade.Student!.FullName, grade.Course!.Name, grade.LetterGrade))
            .Select(grade => (IManagementItemDto)new GradeResponseDto(grade.Id, new GradeValuesDto(
                grade.GradeCode,
                grade.StudentId.ToString(),
                grade.Student?.FullName ?? "—",
                grade.CourseId.ToString(),
                grade.Course?.Name ?? "—",
                grade.Student?.DepartmentId.ToString() ?? "",
                grade.Student?.Department?.Name ?? "—",
                grade.Score.ToString("0.0"),
                grade.LetterGrade,
                grade.AcademicYear,
                grade.Term,
                grade.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct) =>
        throw new InvalidOperationException("Grades are generated automatically from students and cannot be added manually.");
    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct) { var entity = await RequiredEntityAsync(Db.GradeRecords, id, ct); values["gradeCode"] = entity.GradeCode; values["studentId"] = entity.StudentId.ToString(); values["courseId"] = entity.CourseId.ToString(); var period = await CurrentPeriodAsync(ct); if (entity.AcademicYear != period.AcademicYear || entity.Term != period.Term) throw new InvalidOperationException("Completed-semester grades are read-only in Records history."); await BuildAsync(entity, values, ct); Touch(entity); return await SaveUpdatedAsync(id, values, ct); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.GradeRecords.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) => Db.Remove(entity);
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct)
    {
        var grade = (GradeRecord)entity;
        var period = await CurrentPeriodAsync(ct);
        if (grade.AcademicYear != period.AcademicYear || grade.Term != period.Term) throw new InvalidOperationException("Completed-semester grades are permanent Records history and cannot be removed.");
    }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new GradeResponseDto(id, new GradeValuesDto(
            Get(values, "gradeCode"),
            Get(values, "studentId"),
            Get(values, "student"),
            Get(values, "courseId"),
            Get(values, "course"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "score"),
            Get(values, "grade"),
            Get(values, "academicYear"),
            Get(values, "term", "Semester 1"),
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
        entity.Score = DecimalInRange(values, "score", 0, 100);
        entity.LetterGrade = await LetterAsync(entity.Score, ct);
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
    private async Task<string> LetterAsync(decimal score, CancellationToken ct) { var settings = await Db.SystemSettings.Where(x => x.Section == "grade-rules").ToDictionaryAsync(x => x.Key, x => x.Value, ct); return GradeThresholds.From(settings).Letter(score); }
    private async Task<(string AcademicYear, string Term)> CurrentPeriodAsync(CancellationToken ct)
    {
        var settings = await Db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, ct);
        return (settings.GetValueOrDefault("academic-year:currentYear", "2026\u20132027"), settings.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }
}
