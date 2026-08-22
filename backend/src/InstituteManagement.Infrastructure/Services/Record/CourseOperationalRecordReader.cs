using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class CourseOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "courses";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var courses = await db.Courses.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.CourseCode).ToListAsync(cancellationToken);
        var ids = courses.Select(x => x.Id).ToList();
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.CourseId)).ToListAsync(cancellationToken);
        return courses.Select(course =>
        {
            var completed = sessions.Where(x => x.CourseId == course.Id).ToList();
            var events = completed.Select(x => (x.UpdatedAtUtc, Create(("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Year", $"Year {x.YearLevel}"), ("Teacher", x.TeacherName), ("Classroom", x.ClassroomCode), ("Attendance", $"{x.PresentCount} present · {x.LateCount} late · {x.AbsentCount} absent · {x.ExcusedCount} permission"), ("Students", StudentSummary(x.StudentAttendanceJson)))))
                .OrderByDescending(x => x.Item1).ToList();
            return new OperationalRecordDto(course.Id, "Course", course.Name, course.CourseCode, course.IsActive ? "Active" : "Inactive", $"{completed.Count} completed timetable classes", events.Count == 0 ? null : events[0].Item1, events.Select(x => x.Item2).ToList());
        }).ToList();
    }

    private static string StudentSummary(string json)
    {
        try { return string.Join("; ", (JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []).Select(x => $"{x.StudentName}: {x.Status}")); }
        catch (JsonException) { return "Attendance snapshot unavailable"; }
    }
}
