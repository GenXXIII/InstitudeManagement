using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class TeacherOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "teachers";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var teachers = await db.Teachers.AsNoTracking().Include(x => x.Department)
            .Where(x => !departmentId.HasValue || x.DepartmentId == departmentId)
            .OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var ids = teachers.Select(x => x.Id).ToList();
        var assignments = await db.TeacherAssignments.AsNoTracking().Include(x => x.Department)
            .Where(x => ids.Contains(x.TeacherId) && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.TeacherId.HasValue && ids.Contains(x.TeacherId.Value) && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.TeacherId)).ToListAsync(cancellationToken);

        return teachers.Select(teacher =>
        {
            var completed = sessions.Where(x => x.TeacherId == teacher.Id).ToList();
            var assignmentEvents = assignments.Where(x => x.TeacherId == teacher.Id).Select(assignment =>
            {
                var relatedCourses = courseAssignments.Where(x => x.TeacherId == teacher.Id && x.AcademicYear == assignment.AcademicYear && x.Semester == assignment.Semester && (!assignment.DepartmentId.HasValue || x.DepartmentId == assignment.DepartmentId)).ToList();
                var years = string.Join(", ", relatedCourses.Select(x => $"Year {x.YearLevel}").Distinct().OrderBy(x => x));
                var status = TeacherPresence.Attendance(teacher.Status, assignment.Status);
                return (assignment.UpdatedAtUtc, Create(
                    ("Activity", "Teacher assignment"), ("Academic year", assignment.AcademicYear), ("Term", assignment.Semester),
                    ("Date", assignment.UpdatedAtUtc.ToString("yyyy-MM-dd")), ("Time", assignment.UpdatedAtUtc.ToString("HH:mm")),
                    ("Year", string.IsNullOrWhiteSpace(years) ? "Not scheduled" : years),
                    ("Department", assignment.Department?.Name ?? teacher.Department?.Name ?? "Institute-wide"),
                    ("Assigned courses", relatedCourses.Count.ToString()), ("Teacher attendance", status), ("Assignment status", assignment.Status)));
            });
            var sessionEvents = completed.Select(x => (x.UpdatedAtUtc, Create(
                ("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term),
                ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"),
                ("Year", $"Year {x.YearLevel}"), ("Course", x.CourseName), ("Classroom", x.ClassroomCode),
                ("Teacher attendance", x.TeacherAttendanceStatus), ("Session status", TeacherPresence.SessionStatus(x.TeacherAttendanceStatus)), ("Reason", TeacherPresence.Reason(x.TeacherAttendanceStatus)), ("Present", (x.PresentCount + x.LateCount).ToString()),
                ("Permission", x.ExcusedCount.ToString()), ("Absent", x.AbsentCount.ToString()),
                ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"),
                ("Students", StudentSummary(x.StudentAttendanceJson)))));
            var events = assignmentEvents.Concat(sessionEvents).OrderByDescending(x => x.Item1).ToList();
            var attendanceStatus = TeacherPresence.Attendance(teacher.Status);
            return new OperationalRecordDto(teacher.Id, "Teacher", teacher.FullName, teacher.TeacherCode, attendanceStatus,
                $"{completed.Count} recorded timetable periods", events.Count == 0 ? null : events[0].Item1,
                events.Select(x => x.Item2).ToList(), Code: teacher.TeacherCode, PhotoDataUrl: teacher.PhotoDataUrl,
                Department: teacher.Department?.Name ?? "Institute-wide", ResourceId: teacher.Id);
        }).ToList();
    }

    private static string StudentSummary(string json)
    {
        try { return string.Join("; ", (JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []).Select(x => $"{x.StudentName}: {x.Status}")); }
        catch (JsonException) { return "Attendance snapshot unavailable"; }
    }

}
