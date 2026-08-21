using InstituteManagement.Application.Abstractions;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Attendance;

public sealed class AttendanceService(InstituteDbContext db, InstituteCache cache) : IAttendanceService
{
    public async Task RecordAsync(Guid studentId, string status, CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        if (student.Status == "Inactive") throw new InvalidOperationException("Inactive students cannot receive attendance.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.Date == today, cancellationToken);
        if (record is null) { record = new AttendanceRecord { StudentId = studentId, Date = today }; db.AttendanceRecords.Add(record); }
        record.CheckedInAt = TimeOnly.FromDateTime(DateTime.Now); record.Status = status; record.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { ResourceId = record.Id, Type = "Attendance", Subject = student.FullName, Action = status, Details = $"Attendance recorded for {today:yyyy-MM-dd}" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync();
    }
}
