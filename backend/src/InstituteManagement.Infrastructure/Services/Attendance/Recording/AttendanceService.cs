using InstituteManagement.Application.Features.Attendance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Attendance;

public sealed class AttendanceService(InstituteDbContext db, InstituteCache cache) : IAttendanceService
{
    private static readonly HashSet<string> AllowedStatuses =
        new(["Present", "Late", "Absent", "Excused"], StringComparer.OrdinalIgnoreCase);

    public async Task RecordAsync(Guid studentId, string status, CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("StudentId is required.", nameof(studentId));
        if (!AllowedStatuses.Contains(status)) throw new ArgumentException("Attendance status is invalid.", nameof(status));

        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        if (student.Status == "Inactive") throw new InvalidOperationException("Inactive students cannot receive attendance.");
        var period = await db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026\u20132027");
        var term = period.GetValueOrDefault("semester:currentTerm", "Semester 1");
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var today = DateOnly.FromDateTime(localNow);
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.Date == today, cancellationToken);
        if (record is null) { record = new AttendanceRecord { AttendanceCode = $"ATT-{Guid.NewGuid():N}", StudentId = studentId, Date = today, AcademicYear = academicYear, Term = term }; db.AttendanceRecords.Add(record); }
        else if (record.AcademicYear != academicYear || record.Term != term) throw new InvalidOperationException("Today's attendance belongs to a completed academic period and is read-only.");

        var rules = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "attendance-rules" || x.Section == "notifications").ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
        var method = rules.GetValueOrDefault("attendance-rules:method", "ID Card");
        var checkedInAt = TimeOnly.FromDateTime(localNow);
        var appliedStatus = status.Trim();
        if (appliedStatus.Equals("Present", StringComparison.OrdinalIgnoreCase))
        {
            var thresholdText = rules.GetValueOrDefault("attendance-rules:lateThresholdMinutes", "15");
            var threshold = int.TryParse(thresholdText, out var configuredThreshold) ? configuredThreshold : 15;
            var shift = InstituteManagement.Domain.Timetables.AcademicTimetablePolicy.FindShift(student.Shift);
            if (shift is not null && checkedInAt > shift.StartsAt.AddMinutes(threshold)) appliedStatus = "Late";
        }
        record.CheckedInAt = checkedInAt; record.Status = appliedStatus; record.Method = method; record.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = record.Id, Type = "Attendance", Subject = student.FullName, Action = record.Status, Details = $"Attendance recorded for {today:yyyy-MM-dd} - {academicYear} - {term}" });
        if (record.Status is "Late" or "Absent" && Enabled(rules, "notifications:attendanceAlerts", true))
        {
            if (Enabled(rules, "attendance-rules:notifyAdministrator", true))
                db.Notifications.Add(new Notification { Title = $"Attendance {record.Status.ToLowerInvariant()}", Message = $"{student.FullName} was marked {record.Status}.", Severity = record.Status == "Absent" ? "Warning" : "Info" });
            if (Enabled(rules, "attendance-rules:notifyTeacher", true))
            {
                var teacher = await db.ScheduleEntries.AsNoTracking()
                    .Where(entry => entry.DayOfWeek == today.DayOfWeek && entry.YearLevel == student.YearLevel && entry.Course!.DepartmentId == student.DepartmentId && entry.Status != "Cancelled")
                    .Select(entry => entry.Teacher!.FullName)
                    .FirstOrDefaultAsync(cancellationToken);
                db.Notifications.Add(new Notification { Title = "Teacher attendance alert", Message = $"{teacher ?? "Assigned teacher"}: {student.FullName} was marked {record.Status}.", Severity = record.Status == "Absent" ? "Warning" : "Info" });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private static bool Enabled(IReadOnlyDictionary<string, string> values, string key, bool fallback) =>
        bool.TryParse(values.GetValueOrDefault(key), out var enabled) ? enabled : fallback;
}
