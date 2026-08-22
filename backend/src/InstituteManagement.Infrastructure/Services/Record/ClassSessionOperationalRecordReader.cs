using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassSessionOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "sessions";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var sessions = await db.ClassSessionRecords.AsNoTracking()
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId)
            .OrderByDescending(x => x.SessionDate).ThenByDescending(x => x.StartsAt)
            .ToListAsync(cancellationToken);
        return sessions.Select(session =>
        {
            var time = $"{session.StartsAt:HH:mm} – {session.EndsAt:HH:mm}";
            var activities = new List<Dictionary<string, string>>
            {
                Create(("Activity", "Completed class"), ("Date", session.SessionDate.ToString("yyyy-MM-dd")), ("Time", time), ("Course", session.CourseName), ("Year", $"Year {session.YearLevel}"), ("Teacher", session.TeacherName), ("Classroom", session.ClassroomCode), ("Academic year", session.AcademicYear), ("Term", session.Term), ("Attendance", $"{session.PresentCount} present · {session.LateCount} late · {session.AbsentCount} absent · {session.ExcusedCount} permission"))
            };
            activities.AddRange(Deserialize(session.StudentAttendanceJson).Select(student => Create(
                ("Activity", "Student attendance"),
                ("Date", session.SessionDate.ToString("yyyy-MM-dd")),
                ("Time", time),
                ("Student", student.StudentName),
                ("StudentCode", student.StudentCode),
                ("Academic year", session.AcademicYear),
                ("Term", session.Term),
                ("Attendance", student.Status),
                ("Check in", string.IsNullOrWhiteSpace(student.CheckedInAt) ? "No check-in" : student.CheckedInAt))));
            return new OperationalRecordDto(
                session.Id,
                "Session",
                $"{session.CourseName} · Year {session.YearLevel}",
                $"{session.SessionDate:yyyy-MM-dd} · Room {session.ClassroomCode}",
                "Completed",
                $"{time} · {session.TeacherName} · {session.StudentCount} students · {session.PresentCount + session.LateCount} came · {session.AbsentCount} absent",
                session.UpdatedAtUtc,
                activities,
                session.ClassSessionRecordCode);
        }).ToList();
    }

    private static IReadOnlyList<SessionStudentSnapshot> Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
