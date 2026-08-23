using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class CourseOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "courses";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.Courses.AsNoTracking().Include(x => x.Teacher).Include(x => x.Department).Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken); var time = TimeOnly.FromDateTime(now);
        var currentCourseIds = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled" && x.Status != "Completed" && x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .Select(x => x.CourseId)
            .ToHashSetAsync(cancellationToken);
        var courses = await query.Where(x => currentCourseIds.Contains(x.Id)).OrderBy(x => x.CourseCode).ToListAsync(cancellationToken);
        var rows = courses.Select(x => new CourseOperationDto(x.Id, x.Name, x.CourseCode, x.Teacher?.FullName ?? "—", x.Department?.Name ?? "—", x.Capacity, "Running"))
            .OrderBy(x => x.Status == "Running" ? 0 : 1)
            .ThenBy(x => x.CourseCode)
            .ToList();
        var catalogCount = await query.CountAsync(cancellationToken);
        var metrics = new List<MetricDto> { new("Running", rows.Count.ToString(), "Current timetable period", "green"), new("Teachers", rows.Select(x => x.Teacher).Distinct().Count().ToString(), "Assigned right now"), new("Catalog", catalogCount.ToString(), "All active courses"), new("Available", (catalogCount - rows.Count).ToString(), "Not in this period", "violet") };
        return new OperationDto(Module, $"Course operations · {context.Scope}", "Courses and assigned teachers from the timetable period currently in progress.", metrics, context.Activity, context.Attention, Courses: rows);
    }
}
