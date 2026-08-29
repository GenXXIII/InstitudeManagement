using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class CourseOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "courses";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var assignments = await db.CourseAssignments.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Department)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .OrderBy(x => x.Course!.CourseCode)
            .ToListAsync(cancellationToken);
        var assignmentCourseIds = assignments.Select(x => x.CourseId).ToList();
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var currentCourseIds = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled"
                && enrolledTimetableIds.Contains(x.Id)
                && assignmentCourseIds.Contains(x.CourseId)
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt)
            .Select(x => x.CourseId)
            .ToHashSetAsync(cancellationToken);
        if (!selection.IsRunning) currentCourseIds.Clear();
        var courses = assignments.Where(x => currentCourseIds.Contains(x.CourseId) && x.Course is not null).ToList();
        var state = selection.IsRunning ? "Running" : "Scheduled";
        var rows = courses.Select(x => new CourseOperationDto(x.CourseId, x.Course!.Name, x.Course.CourseCode, x.Teacher?.FullName ?? "—", x.Department?.Name ?? "—", x.Capacity, state))
            .OrderBy(x => x.Status == "Running" ? 0 : 1)
            .ThenBy(x => x.CourseCode)
            .ToList();
        var catalogCount = assignments.Count;
        var timing = selection.IsRunning ? "Current" : "Next";
        var metrics = new List<MetricDto> { new(state, rows.Count.ToString(), $"{timing} timetable period", "green"), new("Teachers", rows.Select(x => x.Teacher).Distinct().Count().ToString(), selection.IsRunning ? "Assigned right now" : "Assigned next"), new("Catalog", catalogCount.ToString(), "All active courses"), new("Available", (catalogCount - rows.Count).ToString(), "Not in this period", "violet") };
        return new OperationDto(Module, $"Course operations · {context.Scope}", $"Courses and assigned teachers from the {timing.ToLowerInvariant()} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}).", metrics, context.Activity, context.Attention, Courses: rows);
    }
}
