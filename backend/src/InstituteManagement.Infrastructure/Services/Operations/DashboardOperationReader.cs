using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class DashboardOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "dashboard";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var students = await db.StudentEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var teachers = await db.TeacherAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var rooms = await db.ClassroomAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var courses = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var courseAssignments = courses.ToDictionary(x => x.CourseId);
        var courseIds = courseAssignments.Keys.ToList();
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(localNow);
        var shift = selection.Shift;
        var period = selection.Period;

        var focusedSchedules = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled"
                && enrolledTimetableIds.Contains(x.Id)
                && courseIds.Contains(x.CourseId)
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt)
            .Select(x => new
            {
                x.CourseId,
                x.TeacherId,
                x.ClassroomId,
                x.YearLevel,
                x.StartsAt,
                x.EndsAt
            })
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) focusedSchedules.Clear();
        var cohorts = focusedSchedules.Select(x => (courseAssignments[x.CourseId].DepartmentId, x.YearLevel)).ToHashSet();
        var shiftStudents = students.Where(x => x.Shift == shift.Name).ToList();
        var scheduledStudents = shiftStudents.Count(x => cohorts.Contains((x.DepartmentId, x.YearLevel)));
        var studentTotal = students.Count;
        var teacherTotal = teachers.Count;
        var roomTotal = rooms.Count;
        var courseTotal = courses.Count;
        var runningTeachers = focusedSchedules.Select(x => x.TeacherId).Distinct().Count();
        var focusedRoomIds = focusedSchedules.Select(x => x.ClassroomId).Distinct().ToList();
        var occupiedRooms = focusedRoomIds.Count;
        var runningCourses = focusedSchedules.Select(x => x.CourseId).Distinct().Count();
        var assignedRoomNeedsReview = await db.Classrooms.AsNoTracking().AnyAsync(x => focusedRoomIds.Contains(x.Id) && !x.DeviceOnline, cancellationToken);
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
            new("Students", "Enrollment assigned to this timetable", $"{scheduledStudents:N0} / {studentTotal:N0}", $"Period {periodWindow} / enrolled students · {window}", state, "/operation/students", "blue"),
            new("Teachers", "Faculty assigned through Enrollment", $"{runningTeachers:N0} / {teacherTotal:N0}", $"Period {periodWindow} / assigned teachers · {window}", state, "/operation/teachers", "violet"),
            new("Classrooms", "Rooms assigned through Enrollment", $"{occupiedRooms:N0} / {roomTotal:N0}", $"Period {periodWindow} / assigned rooms · {window}", assignedRoomNeedsReview ? "Review" : state, "/operation/classrooms", "cyan"),
            new("Courses", "Courses assigned through Enrollment", $"{runningCourses:N0} / {courseTotal:N0}", $"Period {periodWindow} / assigned courses · {window}", state, "/operation/courses", "green")
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
