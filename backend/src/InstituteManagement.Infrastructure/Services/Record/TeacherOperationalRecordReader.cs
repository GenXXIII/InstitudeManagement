using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class TeacherOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "teachers";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var teachers = await db.Teachers.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var ids = teachers.Select(x => x.Id).ToList();
        var schedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Classroom).Where(x => ids.Contains(x.TeacherId)).ToListAsync(cancellationToken);
        var courses = await db.Courses.AsNoTracking().Where(x => x.TeacherId.HasValue && ids.Contains(x.TeacherId.Value)).ToListAsync(cancellationToken);
        return teachers.Select(teacher =>
        {
            var events = schedules.Where(x => x.TeacherId == teacher.Id).Select(x => (x.UpdatedAtUtc, Create(("Activity", "Timetable"), ("Day", x.DayOfWeek.ToString()), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Course", x.Course?.Name ?? "—"), ("Classroom", x.Classroom?.Code ?? "—"), ("Status", x.Status))))
                .Concat(courses.Where(x => x.TeacherId == teacher.Id).Select(x => (x.UpdatedAtUtc, Create(("Activity", "Course assignment"), ("Course", x.Name), ("Code", x.Code), ("Status", x.IsActive ? "Active" : "Inactive")))))
                .OrderByDescending(x => x.UpdatedAtUtc).ToList();
            return new OperationalRecordDto(teacher.Id, "Teacher", teacher.FullName, teacher.TeacherNumber, teacher.Status, $"{events.Count(x => x.Item2["Activity"] == "Timetable")} timetable entries · {events.Count(x => x.Item2["Activity"] == "Course assignment")} courses", events.Count == 0 ? null : events[0].UpdatedAtUtc, events.Select(x => x.Item2).ToList());
        }).ToList();
    }
}
