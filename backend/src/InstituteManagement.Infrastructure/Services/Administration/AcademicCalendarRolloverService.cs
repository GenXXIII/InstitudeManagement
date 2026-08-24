using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Administration;

public sealed class AcademicCalendarRolloverService(InstituteDbContext db, InstituteCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<bool> ApplyForCurrentDateAsync(CancellationToken cancellationToken)
    {
        var timeZoneId = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Section == "system" && x.Key == "timeZone")
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken) ?? "Asia/Bangkok";
        TimeZoneInfo timeZone;
        try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { timeZone = TimeZoneInfo.Utc; }
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        return await ApplyAsync(today, cancellationToken);
    }

    public async Task<bool> ApplyAsync(DateOnly today, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var settings = await db.SystemSettings
                .Where(x => x.Section == "academic-year" || x.Section == "semester")
                .ToListAsync(cancellationToken);
            var values = settings.ToDictionary(x => $"{x.Section}:{x.Key}", x => x.Value);
            if (!TryDate(values, "academic-year:startsOn", out var academicStart)
                || !TryDate(values, "academic-year:endsOn", out var academicEnd)
                || !TryDate(values, "semester:semester1StartsOn", out var semester1Start)
                || !TryDate(values, "semester:semester1EndsOn", out var semester1End)
                || !TryDate(values, "semester:semester2StartsOn", out var semester2Start)
                || !TryDate(values, "semester:semester2EndsOn", out var semester2End)) return false;

            var changed = false;
            var promoted = 0;
            var graduated = 0;
            var yearsAdvanced = 0;
            while (today > semester2End)
            {
                var oldYear = values.GetValueOrDefault("academic-year:currentYear", $"{academicStart.Year}\u2013{academicEnd.Year}");
                var activeStudents = await db.Students.Where(x => x.Status != "Inactive" && x.YearLevel >= 1).ToListAsync(cancellationToken);
                var graduates = activeStudents.Where(student => student.YearLevel >= 4).ToList();
                foreach (var student in graduates)
                {
                    student.Status = "Inactive";
                    student.UpdatedAtUtc = DateTime.UtcNow;
                    db.AuditLogs.Add(new AuditLog { ResourceId = student.Id, Type = "Student", Subject = student.FullName, Action = "Graduated", Details = $"{student.StudentCode} completed Year 4 and Semester 2 in {oldYear}; removed from current Management and preserved in Record History." });
                }
                var students = activeStudents.Where(student => student.YearLevel < 4).ToList();
                foreach (var student in students) { student.YearLevel++; student.UpdatedAtUtc = DateTime.UtcNow; }
                graduated += graduates.Count;
                promoted += students.Count;
                yearsAdvanced++;

                academicStart = academicStart.AddYears(1);
                academicEnd = academicEnd.AddYears(1);
                semester1Start = semester1Start.AddYears(1);
                semester1End = semester1End.AddYears(1);
                semester2Start = semester2Start.AddYears(1);
                semester2End = semester2End.AddYears(1);
                db.AuditLogs.Add(new AuditLog
                {
                    Type = "Academic calendar",
                    Subject = oldYear,
                    Action = "Year rollover",
                    Details = $"Closed {oldYear}; promoted {students.Count} active Year 1-3 students and graduated {graduates.Count} Year 4 students. Grade, attendance, and completed-class rows remain in history."
                });
                changed = true;
            }

            var activeTerm = today > semester1End ? "Semester 2" : "Semester 1";
            var activeStart = activeTerm == "Semester 1" ? semester1Start : semester2Start;
            var activeEnd = activeTerm == "Semester 1" ? semester1End : semester2End;
            changed |= Set(settings, "academic-year", "currentYear", $"{academicStart.Year}\u2013{academicEnd.Year}");
            changed |= Set(settings, "academic-year", "startsOn", academicStart.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "academic-year", "endsOn", academicEnd.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester1StartsOn", semester1Start.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester1EndsOn", semester1End.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester2StartsOn", semester2Start.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "semester2EndsOn", semester2End.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "currentTerm", activeTerm);
            changed |= Set(settings, "semester", "startsOn", activeStart.ToString("yyyy-MM-dd"));
            changed |= Set(settings, "semester", "endsOn", activeEnd.ToString("yyyy-MM-dd"));

            if (!changed) return false;
            var activeYear = $"{academicStart.Year}\u2013{academicEnd.Year}";
            var (attendanceCreated, gradesCreated) = await CreateActiveStudentLedgersAsync(activeYear, activeTerm, activeStart, cancellationToken);
            if (yearsAdvanced == 0)
            {
                db.AuditLogs.Add(new AuditLog { Type = "Academic calendar", Subject = activeTerm, Action = "Semester rollover", Details = $"Activated {activeTerm}. Previous grade and attendance rows remain in Records history; Management now uses a new active-period ledger." });
            }
            db.Notifications.Add(new Notification
            {
                Title = yearsAdvanced > 0 ? "Academic year advanced" : $"{activeTerm} activated",
                Message = yearsAdvanced > 0 ? $"Advanced {yearsAdvanced} academic year(s), promoted {promoted} students, graduated {graduated} Year 4 students, and created {attendanceCreated} attendance and {gradesCreated} grade rows." : $"{activeTerm} created {attendanceCreated} attendance and {gradesCreated} grade rows; the previous semester is available in Records.",
                Severity = "Info"
            });
            await db.SaveChangesAsync(cancellationToken);
            await cache.InvalidateDashboardAsync(cancellationToken);
            return true;
        }
        finally { Gate.Release(); }
    }

    private static bool TryDate(IReadOnlyDictionary<string, string> values, string key, out DateOnly date) => DateOnly.TryParse(values.GetValueOrDefault(key), out date);

    private async Task<(int Attendance, int Grades)> CreateActiveStudentLedgersAsync(string academicYear, string term, DateOnly startsOn, CancellationToken cancellationToken)
    {
        var trackedStudents = db.ChangeTracker.Entries<Student>()
            .Where(entry => entry.State is not EntityState.Deleted and not EntityState.Detached)
            .ToDictionary(entry => entry.Entity.Id, entry => entry.Entity);
        var students = (await db.Students.AsNoTracking().ToListAsync(cancellationToken))
            .Select(student => trackedStudents.GetValueOrDefault(student.Id, student))
            .Where(student => student.Status != "Inactive")
            .ToList();
        if (students.Count == 0) return (0, 0);
        var existingAttendance = (await db.AttendanceRecords.AsNoTracking().Where(record => record.AcademicYear == academicYear && record.Term == term).Select(record => record.StudentId).ToListAsync(cancellationToken)).ToHashSet();
        var existingGrades = (await db.GradeRecords.AsNoTracking().Where(record => record.AcademicYear == academicYear && record.Term == term).Select(record => record.StudentId).ToListAsync(cancellationToken)).ToHashSet();
        var schedules = await db.ScheduleEntries.AsNoTracking().Include(entry => entry.Course).Where(entry => entry.Status != "Cancelled").OrderBy(entry => entry.TimetableCode).ToListAsync(cancellationToken);
        var method = await db.SystemSettings.AsNoTracking().Where(setting => setting.Section == "attendance-rules" && setting.Key == "method").Select(setting => setting.Value).FirstOrDefaultAsync(cancellationToken) ?? "ID Card";
        var termCode = term.EndsWith('2') ? "S2" : "S1";
        var attendanceCreated = 0;
        var gradesCreated = 0;
        foreach (var student in students)
        {
            var studentCode = student.StudentCode.StartsWith("STU-", StringComparison.OrdinalIgnoreCase) ? student.StudentCode[4..] : student.StudentCode;
            if (!existingAttendance.Contains(student.Id))
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    AttendanceCode = $"ATT-{studentCode}-{academicYear.Replace("\u2013", "-")}-{termCode}", StudentId = student.Id, Date = startsOn,
                    CheckedInAt = RequiredShift(student.Shift).StartsAt,
                    Status = "Present", Method = method, AcademicYear = academicYear, Term = term
                });
                attendanceCreated++;
            }
            if (!existingGrades.Contains(student.Id))
            {
                var courseId = schedules.FirstOrDefault(entry => entry.YearLevel == student.YearLevel && entry.Course?.DepartmentId == student.DepartmentId && AcademicTimetablePolicy.FindShift(entry.DayOfWeek, entry.StartsAt, entry.EndsAt)?.Name == student.Shift)?.CourseId
                    ?? schedules.FirstOrDefault(entry => entry.YearLevel == student.YearLevel && entry.Course?.DepartmentId == student.DepartmentId)?.CourseId;
                if (!courseId.HasValue) continue;
                db.GradeRecords.Add(new GradeRecord { GradeCode = $"GRD-{studentCode}-{academicYear.Replace("\u2013", "-")}-{termCode}", StudentId = student.Id, CourseId = courseId.Value, Score = 0, LetterGrade = "F", AcademicYear = academicYear, Term = term });
                gradesCreated++;
            }
        }
        return (attendanceCreated, gradesCreated);
    }

    private static AcademicShift RequiredShift(string name) =>
        AcademicTimetablePolicy.FindShift(name) ?? throw new InvalidOperationException("Student shift is not configured in the academic timetable policy.");

    private bool Set(List<SystemSetting> settings, string section, string key, string value)
    {
        var setting = settings.FirstOrDefault(x => x.Section == section && x.Key == key);
        if (setting is null)
        {
            setting = new SystemSetting { Section = section, Key = key, Value = value };
            settings.Add(setting);
            db.SystemSettings.Add(setting);
            return true;
        }
        if (setting.Value == value) return false;
        setting.Value = value;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        return true;
    }
}
