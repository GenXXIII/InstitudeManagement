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
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.ClassroomId)).ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var runningIds = selection.IsRunning
            ? await db.ScheduleEntries.AsNoTracking().Where(x => x.Status != "Cancelled" && x.DayOfWeek == selection.Date.DayOfWeek && x.StartsAt == selection.Period.StartsAt && x.EndsAt == selection.Period.EndsAt).Select(x => x.ClassroomId).ToHashSetAsync(cancellationToken)
            : [];
        return rooms.Select(room =>
        {
            var completed = sessions.Where(x => x.ClassroomId == room.Id).OrderByDescending(x => x.UpdatedAtUtc).ToList();
            var events = completed.Select(x => Create(("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term), ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Year", $"Year {x.YearLevel}"), ("Course", x.CourseName), ("Teacher", x.TeacherName), ("Classroom", room.ClassroomCode), ("Present", (x.PresentCount + x.LateCount).ToString()), ("Permission", x.ExcusedCount.ToString()), ("Absent", x.AbsentCount.ToString()), ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"), ("Students", StudentSummary(x.StudentAttendanceJson)))).ToList();
            var status = room.Status is "Inactive" or "Offline" || !room.DeviceOnline ? "Unavailable" : runningIds.Contains(room.Id) ? "In Study" : "Available";
            return new OperationalRecordDto(room.Id, "Classroom", room.ClassroomCode, $"{room.RoomType} · {room.Building}", status, $"{events.Count} completed timetable classes", completed.Count == 0 ? null : completed[0].UpdatedAtUtc, events, Code: room.ClassroomCode, Department: room.Building, ResourceId: room.Id);
        }).ToList();
    }

    private static string StudentSummary(string json)
    {
        try { return string.Join("; ", (JsonSerializer.Deserialize<List<SessionStudentSnapshot>>(json) ?? []).Select(x => $"{x.StudentName}: {x.Status}")); }
        catch (JsonException) { return "Attendance snapshot unavailable"; }
    }
}
