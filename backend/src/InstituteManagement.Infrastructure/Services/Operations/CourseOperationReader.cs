using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
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
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var currentCourseIds = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled"
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt
                && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .Select(x => x.CourseId)
            .ToHashSetAsync(cancellationToken);
        if (!selection.IsRunning) currentCourseIds.Clear();
        var courses = await query.Where(x => currentCourseIds.Contains(x.Id)).OrderBy(x => x.CourseCode).ToListAsync(cancellationToken);
        var state = selection.IsRunning ? "Running" : "Scheduled";
        var rows = courses.Select(x => new CourseOperationDto(x.Id, x.Name, x.CourseCode, x.Teacher?.FullName ?? "—", x.Department?.Name ?? "—", x.Capacity, state))
            .OrderBy(x => x.Status == "Running" ? 0 : 1)
            .ThenBy(x => x.CourseCode)
            .ToList();
        var catalogCount = await query.CountAsync(cancellationToken);
        var timing = selection.IsRunning ? "Current" : "Next";
        var metrics = new List<MetricDto> { new(state, rows.Count.ToString(), $"{timing} timetable period", "green"), new("Teachers", rows.Select(x => x.Teacher).Distinct().Count().ToString(), selection.IsRunning ? "Assigned right now" : "Assigned next"), new("Catalog", catalogCount.ToString(), "All active courses"), new("Available", (catalogCount - rows.Count).ToString(), "Not in this period", "violet") };
        return new OperationDto(Module, $"Course operations · {context.Scope}", $"Courses and assigned teachers from the {timing.ToLowerInvariant()} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}).", metrics, context.Activity, context.Attention, Courses: rows);
    }
}
