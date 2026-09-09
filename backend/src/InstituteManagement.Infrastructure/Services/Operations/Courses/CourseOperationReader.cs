using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class CourseOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentSourceService enrollmentSource) : IOperationModuleReader
{
    public string Module => "courses";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var source = await enrollmentSource.GetAsync(departmentId, cancellationToken);
        var current = source.Timetables
            .Where(enrollment => enrollment.ScheduleEntry!.Shift == shift.Name
                && enrollment.ScheduleEntry.DayOfWeek == selection.Date.DayOfWeek
                && enrollment.ScheduleEntry.StartsAt == period.StartsAt
                && enrollment.ScheduleEntry.EndsAt == period.EndsAt)
            .ToList();
        if (!selection.IsRunning) current.Clear();

        var rows = current.GroupBy(enrollment => enrollment.CourseId).Select(group =>
        {
            var enrollment = group.First();
            var course = enrollment.Course!;
            var teacher = enrollment.Teacher!;
            var classroom = enrollment.Classroom!;
            var classrooms = group.Select(item => item.Classroom!.ClassroomCode).Distinct().Order().ToList();
            var attendance = TeacherPresence.Attendance(teacher.Status);
            var fixedClassroomStatus = FixedClassroomStatus(classroom.Status, classroom.DeviceOnline);
            var status = fixedClassroomStatus ?? (TeacherPresence.IsPresent(attendance) ? "Running" : "Available");
            var detail = fixedClassroomStatus is null
                ? TeacherPresence.Reason(attendance)
                : $"Classroom {classroom.ClassroomCode} is {fixedClassroomStatus.ToLowerInvariant()} and the course cannot run.";
            return new CourseOperationDto(
                course.Id,
                course.Name,
                course.CourseCode,
                codeFormat.Derive(course.CourseCode, "course", "operation"),
                teacher.FullName,
                string.Join(", ", classrooms),
                course.Department?.Name ?? "—",
                course.Capacity,
                status,
                attendance,
                detail);
        })
            .OrderBy(x => x.Status == "Running" ? 0 : 1)
            .ThenBy(x => x.CourseCode)
            .ToList();

        var catalogCount = source.Timetables.Select(enrollment => enrollment.CourseId).Distinct().Count();
        var timing = selection.IsRunning ? "Current" : "Next";
        var running = rows.Count(x => x.Status == "Running");
        var available = rows.Count(x => x.Status == "Available");
        var blocked = rows.Count(x => x.Status is "Maintenance" or "Unavailable");
        var metrics = new List<MetricDto>
        {
            new("Running", running.ToString(), "Teacher present", "green"),
            new("Available", available.ToString(), "Teacher absent or permission", "red"),
            new("Blocked", blocked.ToString(), "Classroom maintenance or unavailable", "amber"),
            new("Teachers", rows.Select(x => x.Teacher).Distinct().Count().ToString(), selection.IsRunning ? "Assigned right now" : "Assigned next"),
            new("Not scheduled", (catalogCount - rows.Count).ToString(), "Not in this period", "violet")
        };
        return new OperationDto(Module, $"Course operations · {context.Scope}", $"Courses from matched Student Enrollment and Timetable Enrollment in the {timing.ToLowerInvariant()} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}). A course runs only when its enrolled teacher is present and its enrolled classroom is Available.", metrics, context.Activity, context.Attention, Courses: rows);
    }

    private static string? FixedClassroomStatus(string? status, bool? deviceOnline) => status switch
    {
        "Maintenance" or "Reserved" => "Maintenance",
        "Unavailable" or "Inactive" => "Unavailable",
        _ when deviceOnline != true => "Unavailable",
        _ => null
    };
}
