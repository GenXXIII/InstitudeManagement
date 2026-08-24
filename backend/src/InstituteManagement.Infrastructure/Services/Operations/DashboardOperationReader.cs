using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class DashboardOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "dashboard";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var students = db.Students.AsNoTracking()
            .Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var teachers = db.Teachers.AsNoTracking()
            .Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var rooms = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive");
        var courses = db.Courses.AsNoTracking()
            .Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(localNow);
        var shift = selection.Shift;
        var period = selection.Period;

        var focusedSchedules = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled"
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt
                && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .Select(x => new
            {
                x.CourseId,
                x.TeacherId,
                x.ClassroomId,
                x.YearLevel,
                x.StartsAt,
                x.EndsAt,
                DepartmentId = x.Course!.DepartmentId
            })
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) focusedSchedules.Clear();
        var cohorts = focusedSchedules.Select(x => (x.DepartmentId, x.YearLevel)).ToHashSet();
        var shiftStudents = await students
            .Where(x => x.Shift == shift.Name)
            .Select(x => new { x.Id, x.DepartmentId, x.YearLevel })
            .ToListAsync(cancellationToken);
        var scheduledStudents = shiftStudents.Count(x => cohorts.Contains((x.DepartmentId, x.YearLevel)));
        var studentTotal = await students.CountAsync(cancellationToken);
        var teacherTotal = await teachers.CountAsync(cancellationToken);
        var roomTotal = await rooms.CountAsync(cancellationToken);
        var courseTotal = await courses.CountAsync(cancellationToken);
        var runningTeachers = focusedSchedules.Select(x => x.TeacherId).Distinct().Count();
        var focusedRoomIds = focusedSchedules.Select(x => x.ClassroomId).Distinct().ToList();
        var occupiedRooms = focusedRoomIds.Count;
        var runningCourses = focusedSchedules.Select(x => x.CourseId).Distinct().Count();
        var assignedRoomNeedsReview = await rooms.AnyAsync(x => focusedRoomIds.Contains(x.Id) && !x.DeviceOnline, cancellationToken);
        var state = selection.IsRunning ? "Running" : "Next";
        var window = $"{shift.Name} · {selection.Date:dddd} · {shift.StartsAt:HH:mm}-{shift.EndsAt:HH:mm}";
        var periodWindow = $"{period.StartsAt:HH:mm}-{period.EndsAt:HH:mm}";
        var nextShift = selection.IsRunning
            ? AcademicTimetablePolicy.SelectCurrentOrNext(selection.Date.ToDateTime(shift.EndsAt))
            : selection;
        var nextWindow = $"{nextShift.Shift.Name} · {nextShift.Date:dddd} · {nextShift.Shift.StartsAt:HH:mm}-{nextShift.Shift.EndsAt:HH:mm}";
        var dashboardDescription = selection.IsRunning
            ? $"Running shift: {window}. Next shift: {nextWindow}."
            : $"No timetable period is running. Next shift: {nextWindow}.";

        var summary = new List<OperationSummaryDto>
        {
            new("Students", "Enrollment assigned to this timetable", $"{scheduledStudents:N0} / {studentTotal:N0}", $"Period {periodWindow} / active students · {window}", state, "/operation/students", "blue"),
            new("Teachers", "Faculty assigned to this timetable period", $"{runningTeachers:N0} / {teacherTotal:N0}", $"Period {periodWindow} / active teachers · {window}", state, "/operation/teachers", "violet"),
            new("Classrooms", "Rooms assigned to this timetable period", $"{occupiedRooms:N0} / {roomTotal:N0}", $"Period {periodWindow} / institute rooms · {window}", assignedRoomNeedsReview ? "Review" : state, "/operation/classrooms", "cyan"),
            new("Courses", "Courses assigned to this timetable period", $"{runningCourses:N0} / {courseTotal:N0}", $"Period {periodWindow} / active courses · {window}", state, "/operation/courses", "green")
        };
        var metrics = summary.Select(x => new MetricDto(x.Module, x.Value, x.Status, x.Tone)).ToList();

        return new OperationDto(
            Module,
            $"Institute operations dashboard · {context.Scope}",
            dashboardDescription,
            metrics,
            context.Activity,
            context.Attention,
            Summary: summary);
    }
}
