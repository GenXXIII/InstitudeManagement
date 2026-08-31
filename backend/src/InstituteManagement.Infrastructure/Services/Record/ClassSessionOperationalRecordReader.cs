using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassSessionOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "sessions";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var sessions = await db.ClassSessionRecords.AsNoTracking()
            .Include(x => x.ScheduleEntry)
            .Include(x => x.Course)
            .Include(x => x.Teacher)
            .Include(x => x.Classroom)
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId)
            .OrderByDescending(x => x.SessionDate).ThenByDescending(x => x.StartsAt)
            .ToListAsync(cancellationToken);
        return sessions.Select(session =>
        {
            var time = $"{session.StartsAt:HH:mm} – {session.EndsAt:HH:mm}";
            var sessionStatus = TeacherPresence.SessionStatus(session.TeacherAttendanceStatus);
            var statusDetail = TeacherPresence.Reason(session.TeacherAttendanceStatus);
            var sessionCode = ReadableSessionCode(session);
            var activities = new List<Dictionary<string, string>>
            {
                Create(("Activity", "Completed class"), ("Class session code", sessionCode), ("Timetable code", session.ScheduleEntry?.TimetableCode ?? "Not recorded"), ("Date", session.SessionDate.ToString("yyyy-MM-dd")), ("Time", time), ("Course", session.CourseName), ("Course code", session.Course?.CourseCode ?? "Not recorded"), ("Year", $"Year {session.YearLevel}"), ("Teacher", session.TeacherName), ("Teacher code", session.Teacher?.TeacherCode ?? "Not recorded"), ("Classroom", session.ClassroomCode), ("Classroom code", session.Classroom?.ClassroomCode ?? session.ClassroomCode), ("Academic year", session.AcademicYear), ("Term", session.Term), ("Teacher attendance", session.TeacherAttendanceStatus), ("Session status", sessionStatus), ("Reason", statusDetail), ("Attendance", $"{session.PresentCount} present · {session.LateCount} late · {session.AbsentCount} absent · {session.ExcusedCount} permission"))
            };
            activities.AddRange(Deserialize(session.StudentAttendanceJson).Select(student => Create(
                ("Activity", "Student attendance"),
                ("StudentId", student.StudentId.ToString()),
                ("Date", session.SessionDate.ToString("yyyy-MM-dd")),
                ("Time", time),
                ("Student", student.StudentName),
                ("StudentCode", student.StudentCode),
                ("Class session code", sessionCode),
                ("Timetable code", session.ScheduleEntry?.TimetableCode ?? "Not recorded"),
                ("Course code", session.Course?.CourseCode ?? "Not recorded"),
                ("Teacher code", session.Teacher?.TeacherCode ?? "Not recorded"),
                ("Classroom code", session.Classroom?.ClassroomCode ?? session.ClassroomCode),
                ("Academic year", session.AcademicYear),
                ("Term", session.Term),
                ("Teacher attendance", session.TeacherAttendanceStatus),
                ("Session status", sessionStatus),
                ("Reason", statusDetail),
                ("Attendance", student.Status),
                ("Check in", string.IsNullOrWhiteSpace(student.CheckedInAt) ? "No check-in" : student.CheckedInAt))));
            return new OperationalRecordDto(
                session.Id,
                "Session",
                $"{session.CourseName} · Year {session.YearLevel}",
                $"{session.SessionDate:yyyy-MM-dd} · Room {session.ClassroomCode}",
                TeacherPresence.IsPresent(session.TeacherAttendanceStatus) ? "Completed" : "Not held",
                TeacherPresence.IsPresent(session.TeacherAttendanceStatus)
                    ? $"{time} · {session.TeacherName} · {session.StudentCount} students · {session.PresentCount + session.LateCount} came · {session.AbsentCount} absent"
                    : $"{time} · {session.TeacherName} {session.TeacherAttendanceStatus.ToLowerInvariant()} · class not held · room available",
                session.UpdatedAtUtc,
                activities,
                sessionCode);
        }).ToList();
    }

    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? [];
        }
        catch (JsonException) { return []; }
    }

    private static string ReadableSessionCode(ClassSessionRecord session)
    {
        if (session.ClassSessionRecordCode.StartsWith("SES-", StringComparison.OrdinalIgnoreCase)
            && session.ClassSessionRecordCode.Length <= 32)
            return session.ClassSessionRecordCode.ToUpperInvariant();
        var timetable = session.ScheduleEntry?.TimetableCode ?? session.ScheduleEntryId.ToString("N")[..8];
        var suffix = timetable.Contains('-') ? timetable[(timetable.IndexOf('-') + 1)..] : timetable;
        return $"SES-{session.SessionDate:yyyyMMdd}-{suffix}".ToUpperInvariant();
    }
}
