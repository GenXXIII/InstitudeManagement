using System.Globalization;
using System.Text.Json;
using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class OperationalRecordEditService(InstituteDbContext db, InstituteCache cache) : IOperationalRecordEditService
{
    private static readonly HashSet<string> AttendanceStatuses = new(StringComparer.OrdinalIgnoreCase) { "Present", "Late", "Absent", "Excused", "Permission" };

    public async Task UpdateClassSessionAsync(Guid id, UpdateClassSessionRecordDto update, CancellationToken cancellationToken)
    {
        var session = await db.ClassSessionRecords.SingleOrDefaultAsync(record => record.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Class session record not found.");
        var period = await db.SystemSettings.AsNoTracking()
            .Where(setting => (setting.Section == "academic-year" && setting.Key == "currentYear") || (setting.Section == "semester" && setting.Key == "currentTerm"))
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026\u20132027");
        var term = period.GetValueOrDefault("semester:currentTerm", "Semester 1");
        if (session.AcademicYear != academicYear || session.Term != term)
            throw new InvalidOperationException("Closed-semester records are permanent read-only History and cannot be edited.");

        var existing = Deserialize(session.StudentAttendanceJson);
        if (update.Students is null || update.Students.Count != existing.Count || update.Students.Select(student => student.StudentId).Distinct().Count() != update.Students.Count)
            throw new InvalidOperationException("Submit exactly one attendance update for every student in this class session.");
        var updates = update.Students.ToDictionary(student => student.StudentId);
        if (existing.Any(student => !updates.ContainsKey(student.StudentId)))
            throw new InvalidOperationException("The submitted students do not match this class session.");

        var corrected = existing.Select(student => Correct(student, updates[student.StudentId])).ToList();
        session.StudentAttendanceJson = JsonSerializer.Serialize(corrected);
        session.StudentCount = corrected.Count;
        session.PresentCount = corrected.Count(student => student.Status == "Present");
        session.LateCount = corrected.Count(student => student.Status == "Late");
        session.AbsentCount = corrected.Count(student => student.Status == "Absent");
        session.ExcusedCount = corrected.Count(student => student.Status == "Excused");
        session.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            ResourceId = session.Id,
            Type = "Class session",
            Subject = $"{session.CourseName} - Year {session.YearLevel}",
            Action = "Attendance corrected",
            Details = JsonSerializer.Serialize(new { session.ClassSessionRecordCode, session.AcademicYear, session.Term, session.SessionDate, Students = corrected })
        });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }

    private static SessionStudentSnapshot Correct(SessionStudentSnapshot existing, ClassSessionAttendanceUpdateDto update)
    {
        var status = update.Status.Trim();
        if (!AttendanceStatuses.Contains(status))
            throw new InvalidOperationException($"Attendance for {existing.StudentCode} must be Present, Late, Absent, or Permission.");
        status = status.Equals("Permission", StringComparison.OrdinalIgnoreCase) ? "Excused" : AttendanceStatuses.Single(value => value.Equals(status, StringComparison.OrdinalIgnoreCase));
        var checkedInAt = update.CheckedInAt.Trim();
        if (status is "Present" or "Late")
        {
            if (!TimeOnly.TryParseExact(checkedInAt, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                throw new InvalidOperationException($"Check-in time for {existing.StudentCode} is required in HH:mm format.");
            checkedInAt = parsed.ToString("HH:mm");
        }
        else checkedInAt = string.Empty;
        return existing with { Status = status, CheckedInAt = checkedInAt };
    }

    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { throw new InvalidOperationException("This class session contains invalid attendance data and cannot be edited."); }
    }
}
