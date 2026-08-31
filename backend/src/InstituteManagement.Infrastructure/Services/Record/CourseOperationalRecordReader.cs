using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class CourseOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "courses";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var courses = await db.Courses.AsNoTracking().Include(x => x.Department)
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId)
            .OrderBy(x => x.CourseCode).ToListAsync(cancellationToken);
        var ids = courses.Select(x => x.Id).ToList();
        var assignments = await db.CourseAssignments.AsNoTracking().Include(x => x.Department).Include(x => x.Teacher)
            .Where(x => ids.Contains(x.CourseId) && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var departmentIds = assignments.Select(x => x.DepartmentId).Distinct().ToList();
        var enrollments = await db.StudentEnrollments.AsNoTracking()
            .Where(x => departmentIds.Contains(x.DepartmentId) && x.Status == "Active")
            .ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.CourseId)).ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var runningIds = selection.IsRunning
            ? await db.ScheduleEntries.AsNoTracking().Where(x => x.Status != "Cancelled" && x.DayOfWeek == selection.Date.DayOfWeek && x.StartsAt == selection.Period.StartsAt && x.EndsAt == selection.Period.EndsAt).Select(x => x.CourseId).ToHashSetAsync(cancellationToken)
            : [];

        return courses.Select(course =>
        {
            var completed = sessions.Where(x => x.CourseId == course.Id).ToList();
            var assignmentEvents = assignments.Where(x => x.CourseId == course.Id).Select(assignment =>
            {
                var studentCount = enrollments.Count(x => x.DepartmentId == assignment.DepartmentId && x.YearLevel == assignment.YearLevel && x.AcademicYear == assignment.AcademicYear && x.Semester == assignment.Semester);
                return (assignment.UpdatedAtUtc, Create(
                    ("Activity", "Course assignment"), ("Academic year", assignment.AcademicYear), ("Term", assignment.Semester),
                    ("Date", assignment.UpdatedAtUtc.ToString("yyyy-MM-dd")), ("Time", assignment.UpdatedAtUtc.ToString("HH:mm")),
                    ("Year", $"Year {assignment.YearLevel}"), ("Course", course.Name), ("Course code", course.CourseCode),
                    ("Department", assignment.Department?.Name ?? course.Department?.Name ?? "Unassigned"),
                    ("Teacher", assignment.Teacher?.FullName ?? "Not assigned"), ("Enrolled students", studentCount.ToString()),
                    ("Capacity", assignment.Capacity.ToString()), ("Assignment status", assignment.Status)));
            });
            var sessionEvents = completed.Select(x => (x.UpdatedAtUtc, Create(
                ("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term),
                ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"),
                ("Year", $"Year {x.YearLevel}"), ("Teacher", x.TeacherName), ("Classroom", x.ClassroomCode),
                ("Teacher attendance", x.TeacherAttendanceStatus),
                ("Present", (x.PresentCount + x.LateCount).ToString()), ("Permission", x.ExcusedCount.ToString()),
                ("Absent", x.AbsentCount.ToString()),
                ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"),
                ("Students", StudentSummary(x.StudentAttendanceJson)))));
            var events = assignmentEvents.Concat(sessionEvents).OrderByDescending(x => x.Item1).ToList();
            var status = !course.IsActive ? "Unavailable" : runningIds.Contains(course.Id) ? "In Study" : "Available";
            return new OperationalRecordDto(course.Id, "Course", course.Name, course.CourseCode, status,
                $"{completed.Count} completed timetable classes", events.Count == 0 ? null : events[0].Item1,
                events.Select(x => x.Item2).ToList(), Code: course.CourseCode,
                Department: course.Department?.Name ?? "Unassigned", ResourceId: course.Id);
        }).ToList();
    }

    private static string StudentSummary(string json)
    {
        try { return string.Join("; ", (JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []).Select(x => $"{x.StudentName}: {x.Status}")); }
        catch (JsonException) { return "Attendance snapshot unavailable"; }
    }
}
