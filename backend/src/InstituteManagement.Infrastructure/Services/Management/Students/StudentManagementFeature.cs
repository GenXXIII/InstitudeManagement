using System.Text.Json;
using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Students;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Students;

public sealed class StudentManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "students";

    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var students = await Db.Students
            .AsNoTracking()
            .Include(student => student.Department)
            .Where(student => student.Status != "Inactive" && (!departmentId.HasValue || student.DepartmentId == departmentId))
            .ToListAsync(ct);

        return students
            .Where(student => Matches(search, student.FullName, student.StudentCode, student.Department?.Name))
            .Select(student => (IManagementItemDto)new StudentResponseDto(
                student.Id,
                new StudentValuesDto(
                    student.PhotoDataUrl,
                    student.StudentCode,
                    student.FullName,
                    student.Email,
                    student.DepartmentId.ToString(),
                    student.Department?.Name ?? "—",
                    student.YearLevel.ToString(),
                    student.Shift,
                    student.Status,
                    student.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var studentCode = Required(values, "studentCode");
        await EnsureUniqueAsync(Db.Students.Where(student => student.StudentCode == studentCode), "StudentCode", ct);
        var entity = new Student
        {
            StudentCode = studentCode,
            FullName = Required(values, "name"),
            Email = Email(values, "email"),
            PhotoDataUrl = Required(values, "photoDataUrl"),
            DepartmentId = await RelatedIdAsync<Department>(values, "departmentId", ct),
            YearLevel = IntInRange(values, "year", 1, 1, 12),
            Shift = OneOf(values, "shift", "Morning", "Morning", "Afternoon", "Evening"),
            Status = OneOf(values, "status", "Active", "Active", "Inactive")
        };
        await AddStudentOwnedRecordsAsync(entity, ct);
        return await SaveCreatedAsync(entity, values, ct);
    }

    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Students, id, ct);
        var studentCode = Required(values, "studentCode");
        await EnsureUniqueAsync(Db.Students.Where(student => student.Id != id && student.StudentCode == studentCode), "StudentCode", ct);
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && await Db.GradeRecords.AnyAsync(x => x.StudentId == id && x.Course!.DepartmentId != departmentId, ct)) throw new InvalidOperationException("Move or remove this student's grade relationships before changing department.");
        entity.StudentCode = studentCode;
        entity.FullName = Required(values, "name");
        entity.Email = Email(values, "email");
        entity.PhotoDataUrl = Required(values, "photoDataUrl");
        entity.DepartmentId = departmentId;
        entity.YearLevel = IntInRange(values, "year", 1, 1, 12);
        entity.Shift = OneOf(values, "shift", "Morning", "Morning", "Afternoon", "Evening");
        entity.Status = OneOf(values, "status", "Active", "Active", "Inactive");
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }

    private async Task AddStudentOwnedRecordsAsync(Student student, CancellationToken ct)
    {
        var settings = await Db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "academic-year" || setting.Section == "semester" || setting.Section == "attendance-rules" || setting.Section == "grade-rules")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, ct);
        var academicYear = settings.GetValueOrDefault("academic-year:currentYear", "2026\u20132027");
        var term = settings.GetValueOrDefault("semester:currentTerm", "Semester 1");
        var today = DateOnly.FromDateTime(await InstituteLocalTime.NowAsync(Db, ct));
        var attendanceDate = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        if (DateOnly.TryParse(settings.GetValueOrDefault("semester:startsOn"), out var startsOn) && attendanceDate < startsOn) attendanceDate = startsOn;
        if (DateOnly.TryParse(settings.GetValueOrDefault("semester:endsOn"), out var endsOn) && attendanceDate > endsOn) attendanceDate = endsOn;

        var schedule = await Db.ScheduleEntries.AsNoTracking().Include(entry => entry.Course)
            .Where(entry => entry.Status != "Cancelled" && entry.YearLevel == student.YearLevel && entry.Course!.DepartmentId == student.DepartmentId)
            .OrderBy(entry => entry.TimetableCode)
            .ToListAsync(ct);
        var courseId = schedule.FirstOrDefault(entry => ShiftFor(entry.StartsAt) == student.Shift)?.CourseId
            ?? schedule.FirstOrDefault()?.CourseId
            ?? throw new InvalidOperationException("Create a timetable course for this student's department and year before adding the student.");

        var codeSuffix = student.StudentCode.StartsWith("STU-", StringComparison.OrdinalIgnoreCase) ? student.StudentCode[4..] : student.Id.ToString("N");
        var attendance = new AttendanceRecord
        {
            AttendanceCode = $"ATT-{codeSuffix}", StudentId = student.Id, Date = attendanceDate,
            CheckedInAt = student.Shift switch { "Afternoon" => new TimeOnly(14, 0), "Evening" => new TimeOnly(17, 30), _ => new TimeOnly(7, 30) },
            Status = "Present", Method = settings.GetValueOrDefault("attendance-rules:method", "ID Card"), AcademicYear = academicYear, Term = term
        };
        var grade = new GradeRecord
        {
            GradeCode = $"GRD-{codeSuffix}", StudentId = student.Id, CourseId = courseId, Score = 0,
            LetterGrade = "F", AcademicYear = academicYear, Term = term
        };
        Db.AttendanceRecords.Add(attendance);
        Db.GradeRecords.Add(grade);
        Db.AuditLogs.Add(new AuditLog { ResourceId = attendance.Id, Type = "Attendance", Subject = attendance.AttendanceCode, Action = "Created from student", Details = JsonSerializer.Serialize(new { attendance.AttendanceCode, attendance.StudentId, attendance.Date, attendance.CheckedInAt, attendance.Status, attendance.Method, attendance.AcademicYear, attendance.Term }) });
        Db.AuditLogs.Add(new AuditLog { ResourceId = grade.Id, Type = "Grade", Subject = grade.GradeCode, Action = "Created from student", Details = JsonSerializer.Serialize(new { grade.GradeCode, grade.StudentId, grade.CourseId, grade.Score, grade.LetterGrade, grade.AcademicYear, grade.Term }) });
    }

    private static string ShiftFor(TimeOnly start) => start < new TimeOnly(13, 0) ? "Morning" : start < new TimeOnly(17, 30) ? "Afternoon" : "Evening";

    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Students.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((Student)entity).Status = "Inactive"; Touch(entity); }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new StudentResponseDto(id, new StudentValuesDto(
            Get(values, "photoDataUrl"),
            Get(values, "studentCode"),
            Get(values, "name"),
            Get(values, "email"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "year"),
            Get(values, "shift", "Morning"),
            Get(values, "status", "Active"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
}
