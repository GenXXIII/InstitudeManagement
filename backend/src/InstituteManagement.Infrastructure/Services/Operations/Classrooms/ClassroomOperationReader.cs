using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class ClassroomOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentSourceService enrollmentSource) : IOperationModuleReader
{
    public string Module => "classrooms";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var source = await enrollmentSource.GetAsync(departmentId, cancellationToken);
        var classrooms = source.Timetables
            .GroupBy(enrollment => enrollment.ClassroomId)
            .Select(group => group.First())
            .OrderBy(enrollment => enrollment.Classroom!.ClassroomCode)
            .ToList();
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var currentTimetables = source.Timetables
            .Where(enrollment => enrollment.ScheduleEntry!.Shift == shift.Name
                && enrollment.ScheduleEntry.DayOfWeek == selection.Date.DayOfWeek
                && enrollment.ScheduleEntry.StartsAt == period.StartsAt
                && enrollment.ScheduleEntry.EndsAt == period.EndsAt)
            .ToList();
        if (!selection.IsRunning) currentTimetables.Clear();

        var rows = classrooms.Select(enrollment =>
        {
            var room = enrollment.Classroom!;
            var current = currentTimetables.FirstOrDefault(item => item.ClassroomId == enrollment.ClassroomId);
            var fixedStatus = FixedStatus(room.Status, room.DeviceOnline);
            var operationCode = codeFormat.Derive(room.ClassroomCode, "classroom", "operation");
            if (current is null)
                return new ClassroomOperationDto(room.Id, room.ClassroomCode, operationCode, room.RoomType, Floor(room.ClassroomCode), room.Building, room.Capacity, room.DeviceOnline ? "Online" : "Offline", fixedStatus ?? "Available", "No course in this period", "—", "Not scheduled", FixedStatusDetail(fixedStatus));

            var attendance = TeacherPresence.Attendance(current.Teacher!.Status);
            var running = fixedStatus is null && TeacherPresence.IsPresent(attendance);
            var detail = fixedStatus is not null
                ? FixedStatusDetail(fixedStatus)
                : running
                ? $"{current.Course!.Name} is running with {current.Teacher.FullName}."
                : !TeacherPresence.IsPresent(attendance)
                    ? $"{current.Course!.Name} is enrolled, but {current.Teacher.FullName} is {attendance.ToLowerInvariant()}; the course is not running."
                    : $"{current.Course!.Name} is enrolled but is not running.";
            return new ClassroomOperationDto(room.Id, room.ClassroomCode, operationCode, room.RoomType, Floor(room.ClassroomCode), room.Building, room.Capacity, room.DeviceOnline ? "Online" : "Offline", fixedStatus ?? (running ? "Running" : "Available"), current.Course!.Name, current.Teacher.FullName, attendance, detail);
        }).OrderBy(x => x.Room).ToList();

        var runningCount = rows.Count(x => x.Status == "Running");
        var maintenanceCount = rows.Count(x => x.Status == "Maintenance");
        var unavailableCount = rows.Count(x => x.Status == "Unavailable");
        var availableCount = rows.Count(x => x.Status == "Available");
        var metrics = new List<MetricDto>
        {
            new("Running", runningCount.ToString(), "Teacher present", "green"),
            new("Maintenance", maintenanceCount.ToString(), "Fixed management status", "amber"),
            new("Unavailable", unavailableCount.ToString(), "Fixed or device offline", "red"),
            new("Available", availableCount.ToString(), "No class running")
        };
        var timing = selection.IsRunning ? "current" : "next";
        return new OperationDto(Module, $"Learning-space operations · {context.Scope}", $"Rooms come from matched Student Enrollment and Timetable Enrollment for the {timing} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}). Maintenance, availability, and device state remain referenced classroom details.", metrics, context.Activity, context.Attention, Classrooms: rows);
    }

    private static string? FixedStatus(string managementStatus, bool deviceOnline) => managementStatus switch
    {
        "Maintenance" or "Reserved" => "Maintenance",
        "Unavailable" => "Unavailable",
        _ when !deviceOnline => "Unavailable",
        _ => null
    };

    private static string FixedStatusDetail(string? status) => status switch
    {
        "Maintenance" => "Maintenance is fixed in Classroom Management and overrides the timetable.",
        "Unavailable" => "Unavailable is fixed in Classroom Management, or the attendance device is offline, and overrides the timetable.",
        _ => "Available for an enrolled timetable course."
    };

    private static int Floor(string code) => char.IsDigit(code.FirstOrDefault()) ? code[0] - '0' : 1;
}
