using System.Text.Json;
using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassroomOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "classrooms";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var rooms = await db.Classrooms.AsNoTracking().OrderBy(x => x.ClassroomCode).ToListAsync(cancellationToken);
        var ids = rooms.Select(x => x.Id).ToList();
        var assignments = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Department)
            .Where(x => ids.Contains(x.ClassroomId) && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var timetableEnrollments = await db.TimetableEnrollments.AsNoTracking()
            .Include(x => x.ScheduleEntry)!.ThenInclude(x => x!.Course)!.ThenInclude(x => x!.Department)
            .Where(x => x.ScheduleEntry != null && ids.Contains(x.ScheduleEntry.ClassroomId) && (!departmentId.HasValue || x.ScheduleEntry.Course!.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking()
            .Where(x => ids.Contains(x.ClassroomId) && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var runningIds = selection.IsRunning
            ? await db.ScheduleEntries.AsNoTracking().Where(x => x.Status != "Cancelled" && x.DayOfWeek == selection.Date.DayOfWeek && x.StartsAt == selection.Period.StartsAt && x.EndsAt == selection.Period.EndsAt).Select(x => x.ClassroomId).ToHashSetAsync(cancellationToken)
            : [];

        return rooms.Select(room =>
        {
            var completed = sessions.Where(x => x.ClassroomId == room.Id).OrderByDescending(x => x.UpdatedAtUtc).ToList();
            var assignmentEvents = assignments.Where(x => x.ClassroomId == room.Id).Select(assignment =>
            {
                var schedules = timetableEnrollments.Where(x => x.ScheduleEntry?.ClassroomId == room.Id && x.AcademicYear == assignment.AcademicYear && x.Semester == assignment.Semester && x.Status == "Active").Select(x => x.ScheduleEntry!).ToList();
                var courseNames = schedules.Select(x => x.Course?.Name ?? "Course").Distinct().OrderBy(x => x).ToList();
                var years = schedules.Select(x => $"Year {x.YearLevel}").Distinct().OrderBy(x => x).ToList();
                return (assignment.UpdatedAtUtc, Create(
                    ("Activity", "Classroom assignment"), ("Academic year", assignment.AcademicYear), ("Term", assignment.Semester),
                    ("Date", assignment.UpdatedAtUtc.ToString("yyyy-MM-dd")), ("Time", assignment.UpdatedAtUtc.ToString("HH:mm")),
                    ("Year", years.Count == 0 ? "Not scheduled" : string.Join(", ", years)),
                    ("Classroom", room.ClassroomCode), ("Building", room.Building), ("Room type", room.RoomType),
                    ("Department", assignment.Department?.Name ?? assignment.Access), ("Course count", courseNames.Count.ToString()),
                    ("Courses", courseNames.Count == 0 ? "No enrolled courses" : string.Join("; ", courseNames)),
                    ("Capacity", assignment.Capacity.ToString()), ("Assignment status", assignment.Status)));
            });
            var sessionEvents = completed.Select(x => (x.UpdatedAtUtc, Create(
                ("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term),
                ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"),
                ("Year", $"Year {x.YearLevel}"), ("Course", x.CourseName), ("Teacher", x.TeacherName),
                ("Teacher attendance", x.TeacherAttendanceStatus), ("Session status", TeacherPresence.SessionStatus(x.TeacherAttendanceStatus)),
                ("Reason", TeacherPresence.Reason(x.TeacherAttendanceStatus)),
                ("Classroom", room.ClassroomCode), ("Present", (x.PresentCount + x.LateCount).ToString()),
                ("Permission", x.ExcusedCount.ToString()), ("Absent", x.AbsentCount.ToString()),
                ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"),
                ("Students", StudentSummary(x.StudentAttendanceJson)))));
            var events = assignmentEvents.Concat(sessionEvents).OrderByDescending(x => x.Item1).ToList();
            var status = room.Status switch
            {
                "Maintenance" => "Maintenance",
                "Unavailable" or "Inactive" => "Unavailable",
                _ when !room.DeviceOnline => "Unavailable",
                _ when runningIds.Contains(room.Id) => "In Study",
                _ => "Available"
            };
            return new OperationalRecordDto(room.Id, "Classroom", room.ClassroomCode, $"{room.RoomType} · {room.Building}", status,
                $"{completed.Count} recorded timetable periods", events.Count == 0 ? null : events[0].Item1,
                events.Select(x => x.Item2).ToList(), Code: room.ClassroomCode, Department: room.Building, ResourceId: room.Id);
        }).ToList();
    }

    private static string StudentSummary(string json)
    {
        try { return string.Join("; ", (JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []).Select(x => $"{x.StudentName}: {x.Status}")); }
        catch (JsonException) { return "Attendance snapshot unavailable"; }
    }
}
