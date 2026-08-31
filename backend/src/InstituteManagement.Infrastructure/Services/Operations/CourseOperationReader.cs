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
        var currentSchedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Teacher)
            .Where(x => x.Status != "Cancelled"
                && enrolledTimetableIds.Contains(x.Id)
                && assignmentCourseIds.Contains(x.CourseId)
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt)
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) currentSchedules.Clear();

        var currentCourseIds = currentSchedules.Select(x => x.CourseId).ToHashSet();
        var teacherIds = currentSchedules.Select(x => x.TeacherId).Distinct().ToList();
        var teacherAssignments = await db.TeacherAssignments.AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId) && x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned")
            .ToListAsync(cancellationToken);
        var courses = assignments.Where(x => currentCourseIds.Contains(x.CourseId) && x.Course is not null).ToList();
        var rows = courses.Select(x =>
        {
            var schedule = currentSchedules.First(item => item.CourseId == x.CourseId);
            var teacher = schedule.Teacher ?? x.Teacher;
            var teacherAssignment = teacherAssignments
                .Where(item => item.TeacherId == schedule.TeacherId && (item.DepartmentId == x.DepartmentId || item.DepartmentId == null))
                .OrderByDescending(item => item.DepartmentId == x.DepartmentId)
                .FirstOrDefault();
            var attendance = TeacherPresence.Attendance(teacher?.Status, teacherAssignment?.Status);
            var status = TeacherPresence.IsPresent(attendance) ? "Running" : "Not running";
            return new CourseOperationDto(x.CourseId, x.Course!.Name, x.Course.CourseCode, teacher?.FullName ?? "—", x.Department?.Name ?? "—", x.Capacity, status, attendance, TeacherPresence.Reason(attendance));
        })
            .OrderBy(x => x.Status == "Running" ? 0 : 1)
            .ThenBy(x => x.CourseCode)
            .ToList();

        var catalogCount = assignments.Count;
        var timing = selection.IsRunning ? "Current" : "Next";
        var running = rows.Count(x => x.Status == "Running");
        var notRunning = rows.Count - running;
        var metrics = new List<MetricDto>
        {
            new("Running", running.ToString(), "Teacher present", "green"),
            new("Not running", notRunning.ToString(), "Teacher absent or permission", "red"),
            new("Teachers", rows.Select(x => x.Teacher).Distinct().Count().ToString(), selection.IsRunning ? "Assigned right now" : "Assigned next"),
            new("Available", (catalogCount - rows.Count).ToString(), "Not in this period", "violet")
        };
        return new OperationDto(Module, $"Course operations · {context.Scope}", $"Courses from the {timing.ToLowerInvariant()} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}). A course runs only when its assigned teacher is present.", metrics, context.Activity, context.Attention, Courses: rows);
    }
}
