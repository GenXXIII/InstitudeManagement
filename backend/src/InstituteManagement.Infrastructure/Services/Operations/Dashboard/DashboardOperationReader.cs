using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Enrollment;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class DashboardOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentSourceService enrollmentSource) : IOperationModuleReader
{
    public string Module => "dashboard";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var source = await enrollmentSource.GetAsync(departmentId, cancellationToken);
        var students = source.Students;
        var timetables = source.Timetables;
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(localNow);
        var shift = selection.Shift;
        var period = selection.Period;

        var focusedSchedules = timetables
            .Where(enrollment => enrollment.ScheduleEntry!.Shift == shift.Name
                && enrollment.ScheduleEntry.DayOfWeek == selection.Date.DayOfWeek
                && enrollment.ScheduleEntry.StartsAt == period.StartsAt
                && enrollment.ScheduleEntry.EndsAt == period.EndsAt)
            .ToList();
        if (!selection.IsRunning) focusedSchedules.Clear();
        var runnableRoomIds = timetables
            .Where(enrollment => enrollment.Classroom!.DeviceOnline && NormalizeClassroomStatus(enrollment.Classroom.Status) == "Available")
            .Select(enrollment => enrollment.ClassroomId)
            .ToHashSet();
        var teacherPresenceBySchedule = focusedSchedules.ToDictionary(
            enrollment => enrollment.ScheduleEntryId,
            enrollment => TeacherPresence.IsPresent(TeacherPresence.Attendance(enrollment.Teacher!.Status)));
        var runningSchedules = focusedSchedules
            .Where(enrollment => runnableRoomIds.Contains(enrollment.ClassroomId) && teacherPresenceBySchedule[enrollment.ScheduleEntryId])
            .ToList();
        var absentTeacherCount = focusedSchedules
            .Where(enrollment => runnableRoomIds.Contains(enrollment.ClassroomId) && !teacherPresenceBySchedule[enrollment.ScheduleEntryId])
            .Select(enrollment => enrollment.TeacherId)
            .Distinct()
            .Count();
        var cohorts = runningSchedules
            .Select(OperationEnrollmentSourceService.TimetableCohort)
            .Where(cohort => cohort.HasValue)
            .Select(cohort => cohort!.Value)
            .ToHashSet();
        var scheduledStudents = students.Count(enrollment => cohorts.Contains(OperationEnrollmentSourceService.StudentCohort(enrollment)));
        var studentTotal = students.Count;
        var teacherTotal = timetables.Select(enrollment => enrollment.TeacherId).Distinct().Count();
        var roomTotal = timetables.Select(enrollment => enrollment.ClassroomId).Distinct().Count();
        var courseTotal = timetables.Select(enrollment => enrollment.CourseId).Distinct().Count();
        var runningTeachers = runningSchedules.Select(enrollment => enrollment.TeacherId).Distinct().Count();
        var focusedRoomIds = runningSchedules.Select(enrollment => enrollment.ClassroomId).Distinct().ToList();
        var occupiedRooms = focusedRoomIds.Count;
        var runningCourses = runningSchedules.Select(enrollment => enrollment.CourseId).Distinct().Count();
        var assignedRoomNeedsReview = focusedSchedules.Any(enrollment => !runnableRoomIds.Contains(enrollment.ClassroomId));
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
            new("Courses", "Courses assigned through Enrollment", $"{runningCourses:N0} / {courseTotal:N0}", $"Period {periodWindow} / courses with a present teacher · {window}", absentTeacherCount > 0 ? "Available" : state, "/operation/courses", "green")
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

    private static string NormalizeClassroomStatus(string status) => status switch
    {
        "Reserved" => "Maintenance",
        "Inactive" => "Unavailable",
        _ => status
    };
}
