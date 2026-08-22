using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Attendance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Attendance;

public sealed class AttendanceManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "attendance";
    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        var records = await Db.AttendanceRecords.AsNoTracking().Include(record => record.Student).ThenInclude(student => student!.Department)
            .Where(record => record.AcademicYear == period.AcademicYear && record.Term == period.Term && record.Student!.Status != "Inactive" && (!departmentId.HasValue || record.Student.DepartmentId == departmentId))
            .OrderByDescending(record => record.CreateAt)
            .ToListAsync(ct);
        return records.Where(record => Matches(search, record.AttendanceCode, record.Student!.FullName, record.Student.StudentCode, record.Status))
            .Select(record => (IManagementItemDto)new AttendanceResponseDto(record.Id, new AttendanceValuesDto(
                record.AttendanceCode,
                record.StudentId.ToString(),
                record.Student?.FullName ?? "—",
                record.Student?.StudentCode ?? "—",
                record.Student?.DepartmentId.ToString() ?? "",
                record.Student?.Department?.Name ?? "—",
                record.Date.ToString("yyyy-MM-dd"),
                record.CheckedInAt?.ToString("HH:mm") ?? "",
                record.Status,
                record.Method,
                record.AcademicYear,
                record.Term,
                record.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await BuildAsync(new AttendanceRecord(), values, ct);
        await EnsureUniqueAsync(
            Db.AttendanceRecords.Where(record => record.StudentId == entity.StudentId && record.Date == entity.Date),
            "Attendance for this student and date",
            ct);
        return await SaveCreatedAsync(entity, values, ct);
    }
    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        if (!await SettingEnabledAsync("attendance-rules", "allowCorrection", true, ct)) throw new InvalidOperationException("Attendance corrections are disabled by Attendance settings.");
        if (await SettingEnabledAsync("attendance-rules", "requireCorrectionReason", false, ct)) Required(values, "correctionReason");
        var entity = await RequiredEntityAsync(Db.AttendanceRecords, id, ct);
        var period = await CurrentPeriodAsync(ct);
        if (entity.AcademicYear != period.AcademicYear || entity.Term != period.Term) throw new InvalidOperationException("Completed-semester attendance is read-only in Records history.");
        await BuildAsync(entity, values, ct);
        await EnsureUniqueAsync(
            Db.AttendanceRecords.Where(record => record.Id != id && record.StudentId == entity.StudentId && record.Date == entity.Date),
            "Attendance for this student and date",
            ct);
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct)
    {
        if (!await SettingEnabledAsync("attendance-rules", "allowCorrection", true, ct)) throw new InvalidOperationException("Attendance removal is disabled by Attendance settings.");
        var attendance = (AttendanceRecord)entity;
        var period = await CurrentPeriodAsync(ct);
        if (attendance.AcademicYear != period.AcademicYear || attendance.Term != period.Term) throw new InvalidOperationException("Completed-semester attendance is permanent Records history and cannot be removed.");
    }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.AttendanceRecords.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) => Db.Remove(entity);
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new AttendanceResponseDto(id, new AttendanceValuesDto(
            Get(values, "attendanceCode"),
            Get(values, "studentId"),
            Get(values, "student"),
            Get(values, "studentCode"),
            Get(values, "departmentId"),
            Get(values, "department"),
            Get(values, "date"),
            Get(values, "checkedInAt"),
            Get(values, "status", "Present"),
            Get(values, "method", "ID Card"),
            Get(values, "academicYear"),
            Get(values, "term", "Semester 1"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private async Task<AttendanceRecord> BuildAsync(AttendanceRecord entity, Dictionary<string, string> values, CancellationToken ct)
    {
        var attendanceCode = Required(values, "attendanceCode");
        await EnsureUniqueAsync(Db.AttendanceRecords.Where(item => item.Id != entity.Id && item.AttendanceCode == attendanceCode), "AttendanceCode", ct);
        entity.AttendanceCode = attendanceCode;
        entity.StudentId = await RelatedIdAsync<Student>(values, "studentId", ct);
        var student = await Db.Students.AsNoTracking().FirstAsync(x => x.Id == entity.StudentId, ct);
        values["student"] = student.FullName;
        values["studentCode"] = student.StudentCode;
        values["departmentId"] = student.DepartmentId.ToString();
        entity.Date = DateOnly.TryParse(Required(values, "date"), out var date)
            ? date
            : throw new ArgumentException("date must be a valid date.");
        var period = await CurrentPeriodAsync(ct);
        if (entity.Date < period.StartsOn || entity.Date > period.EndsOn) throw new InvalidOperationException($"Attendance date must be inside {period.Term} ({period.StartsOn:yyyy-MM-dd} to {period.EndsOn:yyyy-MM-dd}).");
        entity.AcademicYear = period.AcademicYear;
        entity.Term = period.Term;
        var checkedInAt = Get(values, "checkedInAt");
        entity.CheckedInAt = string.IsNullOrWhiteSpace(checkedInAt)
            ? null
            : TimeOnly.TryParse(checkedInAt, out var time)
                ? time
                : throw new ArgumentException("checkedInAt must be a valid time.");
        entity.Status = OneOf(values, "status", "Present", "Present", "Late", "Absent", "Excused");
        var configuredMethod = await SettingValueAsync("attendance-rules", "method", "ID Card", ct);
        entity.Method = OneOf(values, "method", configuredMethod, "ID Card", "Manual", "QR Code", "Biometric");
        if (entity.Status == "Present" && entity.CheckedInAt.HasValue)
        {
            var thresholdText = await SettingValueAsync("attendance-rules", "lateThresholdMinutes", "15", ct);
            var threshold = int.TryParse(thresholdText, out var minutes) ? minutes : 15;
            var firstPeriod = AcademicTimetablePolicy.ForDay(entity.Date.DayOfWeek).First();
            if (entity.CheckedInAt.Value > firstPeriod.StartsAt.AddMinutes(threshold)) entity.Status = "Late";
        }
        values["status"] = entity.Status; values["method"] = entity.Method; values["academicYear"] = entity.AcademicYear; values["term"] = entity.Term;
        if (entity.Status is "Late" or "Absent" && await SettingEnabledAsync("notifications", "attendanceAlerts", true, ct))
        {
            var studentName = student?.FullName ?? "Student";
            if (await SettingEnabledAsync("attendance-rules", "notifyAdministrator", true, ct))
                Db.Notifications.Add(new Notification { Title = $"Attendance {entity.Status.ToLowerInvariant()}", Message = $"{studentName} was marked {entity.Status} on {entity.Date:yyyy-MM-dd}.", Severity = entity.Status == "Absent" ? "Warning" : "Info" });
            if (await SettingEnabledAsync("attendance-rules", "notifyTeacher", true, ct))
            {
                var teacher = await Db.ScheduleEntries.AsNoTracking()
                    .Where(entry => entry.DayOfWeek == entity.Date.DayOfWeek && entry.YearLevel == student!.YearLevel && entry.Course!.DepartmentId == student.DepartmentId && entry.Status != "Cancelled")
                    .Select(entry => entry.Teacher!.FullName)
                    .FirstOrDefaultAsync(ct);
                Db.Notifications.Add(new Notification { Title = "Teacher attendance alert", Message = $"{teacher ?? "Assigned teacher"}: {studentName} was marked {entity.Status}.", Severity = entity.Status == "Absent" ? "Warning" : "Info" });
            }
        }
        return entity;
    }
    private async Task<string> SettingValueAsync(string section, string key, string fallback, CancellationToken ct) => await Db.SystemSettings.AsNoTracking().Where(x => x.Section == section && x.Key == key).Select(x => x.Value).FirstOrDefaultAsync(ct) ?? fallback;
    private async Task<bool> SettingEnabledAsync(string section, string key, bool fallback, CancellationToken ct) { var value = await SettingValueAsync(section, key, fallback.ToString(), ct); return bool.TryParse(value, out var enabled) ? enabled : fallback; }
    private async Task<(string AcademicYear, string Term, DateOnly StartsOn, DateOnly EndsOn)> CurrentPeriodAsync(CancellationToken ct)
    {
        var settings = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "academic-year" || x.Section == "semester").ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, ct);
        var startsOn = DateOnly.TryParse(settings.GetValueOrDefault("semester:startsOn"), out var start) ? start : DateOnly.MinValue;
        var endsOn = DateOnly.TryParse(settings.GetValueOrDefault("semester:endsOn"), out var end) ? end : DateOnly.MaxValue;
        return (settings.GetValueOrDefault("academic-year:currentYear", "2026\u20132027"), settings.GetValueOrDefault("semester:currentTerm", "Semester 1"), startsOn, endsOn);
    }
}
