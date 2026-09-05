using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class ClassroomOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "classrooms";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var classrooms = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Classroom)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId))
            .OrderBy(x => x.Classroom!.ClassroomCode)
            .ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var roomIds = classrooms.Select(room => room.ClassroomId).ToList();
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var enrolledCourseIds = courseAssignments.Select(x => x.CourseId).ToList();
        var currentSchedules = await db.ScheduleEntries.AsNoTracking().Include(entry => entry.Course).Include(entry => entry.Teacher)
            .Where(entry => roomIds.Contains(entry.ClassroomId)
                && enrolledTimetableIds.Contains(entry.Id)
                && enrolledCourseIds.Contains(entry.CourseId)
                && entry.Status != "Cancelled"
                && entry.DayOfWeek == selection.Date.DayOfWeek
                && entry.StartsAt == period.StartsAt && entry.EndsAt == period.EndsAt)
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) currentSchedules.Clear();
        var teacherIds = currentSchedules.Select(x => x.TeacherId).Distinct().ToList();
        var teacherAssignments = await db.TeacherAssignments.AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId) && x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned")
            .ToListAsync(cancellationToken);

        var rows = classrooms.Where(x => x.Classroom is not null).Select(x =>
        {
            var room = x.Classroom!;
            var schedule = currentSchedules.FirstOrDefault(item => item.ClassroomId == x.ClassroomId);
            var fixedStatus = FixedStatus(room.Status, room.DeviceOnline);
            if (schedule is null)
                return new ClassroomOperationDto(x.ClassroomId, room.ClassroomCode, x.EnrollmentCode, room.RoomType, Floor(room.ClassroomCode), room.Building, x.Capacity, room.DeviceOnline ? "Online" : "Offline", fixedStatus ?? "Available", "No course in this period", "—", "Not scheduled", FixedStatusDetail(fixedStatus));

            var department = courseAssignments.First(item => item.CourseId == schedule.CourseId).DepartmentId;
            var teacherAssignment = teacherAssignments
                .Where(item => item.TeacherId == schedule.TeacherId && (item.DepartmentId == department || item.DepartmentId == null))
                .OrderByDescending(item => item.DepartmentId == department)
                .FirstOrDefault();
            var attendance = TeacherPresence.Attendance(schedule.Teacher?.Status, teacherAssignment?.Status);
            var running = fixedStatus is null && TeacherPresence.IsPresent(attendance);
            var detail = fixedStatus is not null
                ? FixedStatusDetail(fixedStatus)
                : running
                ? $"{schedule.Course?.Name ?? "Course"} is running with {schedule.Teacher?.FullName ?? "the assigned teacher"}."
                : !TeacherPresence.IsPresent(attendance)
                    ? $"{schedule.Course?.Name ?? "Course"} is assigned, but {schedule.Teacher?.FullName ?? "the teacher"} is {attendance.ToLowerInvariant()}; the course is not running."
                    : $"{schedule.Course?.Name ?? "Course"} is assigned but is not running.";
            return new ClassroomOperationDto(x.ClassroomId, room.ClassroomCode, x.EnrollmentCode, room.RoomType, Floor(room.ClassroomCode), room.Building, x.Capacity, room.DeviceOnline ? "Online" : "Offline", fixedStatus ?? (running ? "Running" : "Available"), schedule.Course?.Name ?? "Course", schedule.Teacher?.FullName ?? "—", attendance, detail);
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
        return new OperationDto(Module, $"Learning-space operations · {context.Scope}", $"Room status for the {timing} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}). Available rooms follow the timetable; Maintenance and Unavailable remain fixed until changed in Classroom Management.", metrics, context.Activity, context.Attention, Classrooms: rows);
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
        _ => "Available for an assigned timetable course."
    };

    private static int Floor(string code) => char.IsDigit(code.FirstOrDefault()) ? code[0] - '0' : 1;
}
