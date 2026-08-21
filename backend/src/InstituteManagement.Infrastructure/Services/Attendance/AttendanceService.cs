using InstituteManagement.Application.Abstractions;
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.Date == today, cancellationToken);
        if (record is null) { record = new AttendanceRecord { StudentId = studentId, Date = today, AcademicYear = academicYear, Term = term }; db.AttendanceRecords.Add(record); }
        else if (record.AcademicYear != academicYear || record.Term != term) throw new InvalidOperationException("Today's attendance belongs to a completed academic period and is read-only.");
        var method = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "attendance-rules" && x.Key == "method").Select(x => x.Value).FirstOrDefaultAsync(cancellationToken) ?? "ID Card";
        record.CheckedInAt = TimeOnly.FromDateTime(DateTime.Now); record.Status = status.Trim(); record.Method = method; record.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = record.Id, Type = "Attendance", Subject = student.FullName, Action = status, Details = $"Attendance recorded for {today:yyyy-MM-dd} · {academicYear} · {term}" });
        var alerts = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "notifications" && x.Key == "attendanceAlerts").Select(x => x.Value).FirstOrDefaultAsync(cancellationToken);
        if (record.Status is "Late" or "Absent" && (!bool.TryParse(alerts, out var enabled) || enabled)) db.Notifications.Add(new Notification { Title = $"Attendance {record.Status.ToLowerInvariant()}", Message = $"{student.FullName} was marked {record.Status}.", Severity = record.Status == "Absent" ? "Warning" : "Info" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }
}
