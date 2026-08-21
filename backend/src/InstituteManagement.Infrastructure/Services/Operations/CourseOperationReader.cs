using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class CourseOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "courses";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.Courses.AsNoTracking().Include(x => x.Teacher).Include(x => x.Department).Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var courses = await query.OrderBy(x => x.Code).Take(16).ToListAsync(cancellationToken);
        var schedules = db.ScheduleEntries.AsNoTracking().Where(x => x.Status != "Cancelled" && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId));
        var now = DateTime.Now; var time = TimeOnly.FromDateTime(now);
        var running = await schedules.CountAsync(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time, cancellationToken);
        var rows = courses.Select(x => new CourseOperationDto(x.Id, x.Name, x.Code, x.Teacher?.FullName ?? "—", x.Department?.Name ?? "—", x.Capacity, "Active")).ToList();
        var metrics = new List<MetricDto> { new("Courses", (await query.CountAsync(cancellationToken)).ToString(), "Active catalog"), new("Teachers", courses.Select(x => x.TeacherId).Where(x => x.HasValue).Distinct().Count().ToString(), "Assigned faculty", "green"), new("Running", running.ToString(), "Right now"), new("Upcoming", (await schedules.CountAsync(cancellationToken)).ToString(), "Weekly slots", "violet") };
        return new OperationDto(Module, $"Course operations · {context.Scope}", "Track active courses, instructors, rooms, and capacity for the selected department.", metrics, context.Activity, context.Attention, Courses: rows);
    }
}
