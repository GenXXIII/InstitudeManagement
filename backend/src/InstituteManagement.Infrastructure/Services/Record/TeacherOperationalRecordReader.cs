using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class TeacherOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "teachers";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var teachers = await db.Teachers.AsNoTracking().Include(x => x.Department).Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var ids = teachers.Select(x => x.Id).ToList();
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.TeacherId)).ToListAsync(cancellationToken);
        return teachers.Select(teacher =>
        {
            var completed = sessions.Where(x => x.TeacherId == teacher.Id).ToList();
            var events = completed.Select(x => (x.UpdatedAtUtc, Create(("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Year", $"Year {x.YearLevel}"), ("Course", x.CourseName), ("Classroom", x.ClassroomCode), ("Teacher attendance", x.TeacherAttendanceStatus), ("Present", (x.PresentCount + x.LateCount).ToString()), ("Permission", x.ExcusedCount.ToString()), ("Absent", x.AbsentCount.ToString()), ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"), ("Students", StudentSummary(x.StudentAttendanceJson)))))
                .OrderByDescending(x => x.Item1).ToList();
            var attendanceStatus = teacher.Status switch { "On leave" => "Permission", "Inactive" => "Absent", _ => "Present" };
            return new OperationalRecordDto(teacher.Id, "Teacher", teacher.FullName, teacher.TeacherCode, attendanceStatus, $"{completed.Count} completed timetable classes", events.Count == 0 ? null : events[0].Item1, events.Select(x => x.Item2).ToList(), Code: teacher.TeacherCode, PhotoDataUrl: teacher.PhotoDataUrl, Department: teacher.Department?.Name ?? "Institute-wide", ResourceId: teacher.Id);
        }).ToList();
    }

    private static string StudentSummary(string json)
    {
        try { return string.Join("; ", (JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []).Select(x => $"{x.StudentName}: {x.Status}")); }
        catch (JsonException) { return "Attendance snapshot unavailable"; }
    }
}
