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
            .Include(x => x.Teacher)
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
        var runningSchedules = focusedSchedules.Where(schedule =>
        {
            var courseDepartmentId = courseAssignments[schedule.CourseId].DepartmentId;
            var assignment = teachers
                .Where(item => item.TeacherId == schedule.TeacherId
                    && (item.DepartmentId == courseDepartmentId || item.DepartmentId == null))
                .OrderByDescending(item => item.DepartmentId == courseDepartmentId)
                .FirstOrDefault();
            return assignment?.Teacher is not null
                && TeacherPresence.IsPresent(TeacherPresence.Attendance(assignment.Teacher.Status, assignment.Status));
        }).ToList();
        var absentTeacherCount = focusedSchedules
            .Where(schedule => !runningSchedules.Contains(schedule))
            .Select(schedule => schedule.TeacherId)
            .Distinct()
            .Count();
        var cohorts = runningSchedules.Select(x => (courseAssignments[x.CourseId].DepartmentId, x.YearLevel)).ToHashSet();
        var shiftStudents = students.Where(x => x.Shift == shift.Name).ToList();
        var scheduledStudents = shiftStudents.Count(x => cohorts.Contains((x.DepartmentId, x.YearLevel)));
        var studentTotal = students.Count;
        var teacherTotal = teachers.Count;
        var roomTotal = rooms.Count;
        var courseTotal = courses.Count;
        var runningTeachers = runningSchedules.Select(x => x.TeacherId).Distinct().Count();
        var focusedRoomIds = runningSchedules.Select(x => x.ClassroomId).Distinct().ToList();
        var occupiedRooms = focusedRoomIds.Count;
        var runningCourses = runningSchedules.Select(x => x.CourseId).Distinct().Count();
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
        if (absentTeacherCount > 0)
            dashboardDescription += $" {absentTeacherCount} assigned teacher(s) are absent or on permission; their courses are not running and their classrooms remain available.";

        var summary = new List<OperationSummaryDto>
        {
            new("Students", "Enrollment assigned to classes that are actually running", $"{scheduledStudents:N0} / {studentTotal:N0}", $"Period {periodWindow} / students in running classes · {window}", absentTeacherCount > 0 ? "Class not running" : state, "/operation/students", "blue"),
            new("Teachers", "Faculty assigned through Enrollment", $"{runningTeachers:N0} / {teacherTotal:N0}", $"Period {periodWindow} / present assigned teachers · {window}", absentTeacherCount > 0 ? "Absent" : state, "/operation/teachers", "violet"),
            new("Classrooms", "Rooms assigned through Enrollment", $"{occupiedRooms:N0} / {roomTotal:N0}", $"Period {periodWindow} / rooms used by running classes · {window}", absentTeacherCount > 0 ? "Available" : assignedRoomNeedsReview ? "Review" : state, "/operation/classrooms", "cyan"),
            new("Courses", "Courses assigned through Enrollment", $"{runningCourses:N0} / {courseTotal:N0}", $"Period {periodWindow} / courses with a present teacher · {window}", absentTeacherCount > 0 ? "Not running" : state, "/operation/courses", "green")
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
