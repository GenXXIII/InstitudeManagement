using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class CourseOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "courses";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var courses = await db.Courses.AsNoTracking().Where(x => !departmentId.HasValue || x.DepartmentId == departmentId).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var ids = courses.Select(x => x.Id).ToList();
        var schedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Classroom).Include(x => x.Teacher).Where(x => ids.Contains(x.CourseId)).ToListAsync(cancellationToken);
        var grades = await db.GradeRecords.AsNoTracking().Include(x => x.Student).Where(x => ids.Contains(x.CourseId)).ToListAsync(cancellationToken);
        return courses.Select(course =>
        {
            var events = schedules.Where(x => x.CourseId == course.Id).Select(x => (x.UpdatedAtUtc, Create(("Activity", "Timetable"), ("Day", x.DayOfWeek.ToString()), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Teacher", x.Teacher?.FullName ?? "—"), ("Classroom", x.Classroom?.Code ?? "—"), ("Status", x.Status))))
                .Concat(grades.Where(x => x.CourseId == course.Id).Select(x => (x.UpdatedAtUtc, Create(("Activity", "Assessment"), ("Student", x.Student?.FullName ?? "—"), ("Term", x.Term), ("Score", x.Score.ToString("0.0")), ("Grade", x.LetterGrade)))))
                .OrderByDescending(x => x.UpdatedAtUtc).ToList();
            return new OperationalRecordDto(course.Id, "Course", course.Name, course.Code, course.IsActive ? "Active" : "Inactive", $"{events.Count(x => x.Item2["Activity"] == "Timetable")} timetable entries · {events.Count(x => x.Item2["Activity"] == "Assessment")} assessments", events.Count == 0 ? null : events[0].UpdatedAtUtc, events.Select(x => x.Item2).ToList());
        }).ToList();
    }
}
