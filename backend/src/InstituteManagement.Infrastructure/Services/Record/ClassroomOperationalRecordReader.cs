using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class ClassroomOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "classrooms";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var rooms = await db.Classrooms.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var ids = rooms.Select(x => x.Id).ToList();
        var schedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Where(x => ids.Contains(x.ClassroomId)).ToListAsync(cancellationToken);
        return rooms.Select(room =>
        {
            var related = schedules.Where(x => x.ClassroomId == room.Id).OrderByDescending(x => x.UpdatedAtUtc).ToList();
            var events = related.Select(x => Create(("Activity", "Timetable"), ("Day", x.DayOfWeek.ToString()), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Year", $"Year {x.YearLevel}"), ("Course", x.Course?.Name ?? "—"), ("Teacher", x.Teacher?.FullName ?? "—"), ("Status", x.Status))).ToList();
            return new OperationalRecordDto(room.Id, "Classroom", room.Code, room.Building, room.Status, $"{events.Count} timetable entries · {room.Capacity} seats · device {(room.DeviceOnline ? "online" : "offline")}", related.Count == 0 ? null : related[0].UpdatedAtUtc, events);
        }).ToList();
    }
}
