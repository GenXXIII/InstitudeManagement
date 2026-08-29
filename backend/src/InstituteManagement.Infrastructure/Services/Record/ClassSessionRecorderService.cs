using System.Text.Json;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassSessionRecorderService(InstituteDbContext db, InstituteCache cache)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<int> RecordCompletedForCurrentTimeAsync(CancellationToken cancellationToken)
    {
        var timeZoneId = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "system" && x.Key == "timeZone").Select(x => x.Value).FirstOrDefaultAsync(cancellationToken) ?? "Asia/Bangkok";
        TimeZoneInfo timeZone;
        try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { timeZone = TimeZoneInfo.Utc; }
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        return await RecordCompletedAsync(DateOnly.FromDateTime(localNow), TimeOnly.FromDateTime(localNow), timeZone, cancellationToken);
    }

    public async Task<int> RecordCompletedAsync(DateOnly today, TimeOnly localTime, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var period = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "academic-year" || x.Section == "semester").ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
            var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026\u20132027");
            var term = period.GetValueOrDefault("semester:currentTerm", "Semester 1");
            var termStart = DateOnly.TryParse(period.GetValueOrDefault("semester:startsOn"), out var configuredStart) ? configuredStart : today;
            var rules = await db.SystemSettings.AsNoTracking().Where(x => x.Section == "attendance-rules" || x.Section == "notifications").ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
            var autoAbsent = Enabled(rules, "attendance-rules:autoAbsent", true);
            var dailySummary = Enabled(rules, "notifications:dailySummary", true);
            var lastRecorded = await db.ClassSessionRecords.AsNoTracking().Where(x => x.AcademicYear == academicYear && x.Term == term).Select(x => (DateOnly?)x.SessionDate).MaxAsync(cancellationToken);
            var firstDate = lastRecorded ?? today;
            if (firstDate < termStart) firstDate = termStart;

            var courseAssignments = await db.CourseAssignments.AsNoTracking()
                .Where(x => x.AcademicYear == academicYear && x.Semester == term && x.Status == "Active")
                .ToDictionaryAsync(x => x.CourseId, cancellationToken);
            var teacherAssignments = await db.TeacherAssignments.AsNoTracking()
                .Where(x => x.AcademicYear == academicYear && x.Semester == term && x.Status != "Removed" && x.Status != "Unassigned")
                .ToListAsync(cancellationToken);
            var courseIds = courseAssignments.Keys.ToList();
            var enrolledScheduleIds = await db.TimetableEnrollments.AsNoTracking()
                .Where(x => x.AcademicYear == academicYear && x.Semester == term && x.Status == "Active")
                .Select(x => x.ScheduleEntryId)
                .ToListAsync(cancellationToken);
            var schedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Classroom)
                .Where(x => x.Status != "Cancelled" && enrolledScheduleIds.Contains(x.Id) && courseIds.Contains(x.CourseId))
                .ToListAsync(cancellationToken);
            var existing = await db.ClassSessionRecords.AsNoTracking().Where(x => x.SessionDate >= firstDate && x.SessionDate <= today).Select(x => new { x.ScheduleEntryId, x.SessionDate }).ToListAsync(cancellationToken);
            var existingKeys = existing.Select(x => (x.ScheduleEntryId, x.SessionDate)).ToHashSet();
            var recorded = 0;

            for (var date = firstDate; date <= today; date = date.AddDays(1))
            {
                var completed = schedules.Where(x => x.DayOfWeek == date.DayOfWeek && (date < today || x.EndsAt <= localTime));
                foreach (var schedule in completed)
                {
                    if (existingKeys.Contains((schedule.Id, date)) || schedule.Course is null || schedule.Teacher is null || schedule.Classroom is null) continue;
                    var shift = AcademicTimetablePolicy.FindShift(schedule.DayOfWeek, schedule.StartsAt, schedule.EndsAt);
                    if (shift is null) continue;
                    var courseAssignment = courseAssignments[schedule.CourseId];
                    var teacherAssignment = teacherAssignments.FirstOrDefault(x => x.TeacherId == schedule.TeacherId && (x.DepartmentId == courseAssignment.DepartmentId || x.DepartmentId == null));
                    var studentEnrollments = await db.StudentEnrollments.AsNoTracking().Include(x => x.Student)
                        .Where(x => x.AcademicYear == academicYear && x.Semester == term && x.Status == "Active"
                            && x.DepartmentId == courseAssignment.DepartmentId && x.YearLevel == schedule.YearLevel && x.Shift == shift.Name)
                        .OrderBy(x => x.Student!.FullName)
                        .ToListAsync(cancellationToken);
                    var students = studentEnrollments.Where(x => x.Student is not null && x.Student.Status != "Inactive").Select(x => x.Student!).ToList();
                    var studentIds = students.Select(x => x.Id).ToList();
                    var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => studentIds.Contains(x.StudentId) && x.Date == date && x.AcademicYear == academicYear && x.Term == term).ToDictionaryAsync(x => x.StudentId, cancellationToken);
                    var snapshots = students.Select(student =>
                    {
                        var entry = attendance.GetValueOrDefault(student.Id);
                        return new SessionStudentSnapshot(student.Id, student.StudentCode, student.FullName, entry?.Status ?? (autoAbsent ? "Absent" : "Not recorded"), entry?.CheckedInAt?.ToString("HH:mm") ?? "");
                    }).ToList();
                    var endedAtUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(schedule.EndsAt), timeZone);
                    var entity = new ClassSessionRecord
                    {
                        ScheduleEntryId = schedule.Id,
                        SessionDate = date,
                        AcademicYear = academicYear,
                        Term = term,
                        DepartmentId = courseAssignment.DepartmentId,
                        CourseId = schedule.CourseId,
                        TeacherId = schedule.TeacherId,
                        ClassroomId = schedule.ClassroomId,
                        YearLevel = schedule.YearLevel,
                        StartsAt = schedule.StartsAt,
                        EndsAt = schedule.EndsAt,
                        CourseName = schedule.Course.Name,
                        TeacherName = schedule.Teacher.FullName,
                        TeacherAttendanceStatus = TeacherAttendance(teacherAssignment?.Status ?? schedule.Teacher.Status),
                        ClassroomCode = schedule.Classroom.ClassroomCode,
                        StudentCount = snapshots.Count,
                        PresentCount = snapshots.Count(x => x.Status == "Present"),
                        LateCount = snapshots.Count(x => x.Status == "Late"),
                        AbsentCount = snapshots.Count(x => x.Status == "Absent"),
                        ExcusedCount = snapshots.Count(x => x.Status == "Excused"),
                        StudentAttendanceJson = JsonSerializer.Serialize(snapshots),
                        CreateAt = endedAtUtc,
                        UpdatedAtUtc = endedAtUtc
                    };
                    db.ClassSessionRecords.Add(entity);
                    db.AuditLogs.Add(new AuditLog
                    {
                        ResourceId = entity.Id,
                        Type = "Class session",
                        Subject = $"{schedule.Course.Name} · Year {schedule.YearLevel}",
                        Action = "Session completed",
                        Details = $"{date:yyyy-MM-dd} {schedule.StartsAt:HH:mm}-{schedule.EndsAt:HH:mm} · {schedule.Teacher.FullName} · Room {schedule.Classroom.ClassroomCode} · {snapshots.Count} students · {entity.PresentCount} present · {entity.LateCount} late · {entity.AbsentCount} absent · {entity.ExcusedCount} excused",
                        CreateAt = endedAtUtc,
                        UpdatedAtUtc = endedAtUtc
                    });
                    existingKeys.Add((schedule.Id, date));
                    recorded++;
                }
            }

            if (recorded == 0) return 0;
            if (dailySummary) db.Notifications.Add(new Notification { Title = "Daily class summary", Message = $"{recorded:N0} completed class session{(recorded == 1 ? "" : "s")} recorded for {today:yyyy-MM-dd}.", Severity = "Info" });
            await db.SaveChangesAsync(cancellationToken);
            await cache.InvalidateDashboardAsync(cancellationToken);
            return recorded;
        }
        finally { Gate.Release(); }
    }

    private static bool Enabled(IReadOnlyDictionary<string, string> values, string key, bool fallback) =>
        bool.TryParse(values.GetValueOrDefault(key), out var enabled) ? enabled : fallback;

    private static string TeacherAttendance(string status) => status switch { "On leave" => "Permission", "Inactive" => "Absent", _ => "Present" };

}
