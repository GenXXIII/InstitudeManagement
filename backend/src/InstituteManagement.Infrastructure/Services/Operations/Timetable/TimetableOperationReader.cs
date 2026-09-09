using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TimetableOperationReader(
    InstituteDbContext db,
    OperationContextService contextService,
    OperationEnrollmentSourceService enrollmentSource) : IOperationModuleReader
{
    public string Module => "timetable";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var codeFormat = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        var source = await enrollmentSource.GetAsync(departmentId, cancellationToken);
        var timetableEnrollments = source.Timetables
            .OrderBy(enrollment => enrollment.ScheduleEntry!.DayOfWeek)
            .ThenBy(enrollment => enrollment.ScheduleEntry!.StartsAt)
            .ToList();

        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var time = TimeOnly.FromDateTime(now);
        var teacherAttendance = timetableEnrollments.ToDictionary(
            enrollment => enrollment.ScheduleEntryId,
            enrollment => TeacherPresence.Attendance(enrollment.Teacher?.Status));
        var runnableRoomIds = timetableEnrollments
            .Where(enrollment =>
                enrollment.Classroom is not null
                && NormalizeClassroomStatus(enrollment.Classroom.Status) == "Available"
                && enrollment.Classroom.DeviceOnline)
            .Select(enrollment => enrollment.ClassroomId)
            .ToHashSet();

        bool IsCurrent(ScheduleEntry entry) =>
            entry.DayOfWeek == now.DayOfWeek && entry.StartsAt <= time && entry.EndsAt > time;

        var running = timetableEnrollments.Count(enrollment =>
            IsCurrent(enrollment.ScheduleEntry!)
            && TeacherPresence.IsPresent(teacherAttendance[enrollment.ScheduleEntryId])
            && runnableRoomIds.Contains(enrollment.ClassroomId));
        var available = timetableEnrollments.Count(enrollment =>
            IsCurrent(enrollment.ScheduleEntry!)
            && !TeacherPresence.IsPresent(teacherAttendance[enrollment.ScheduleEntryId])
            && runnableRoomIds.Contains(enrollment.ClassroomId));
        var blocked = timetableEnrollments.Count(enrollment =>
            IsCurrent(enrollment.ScheduleEntry!)
            && !runnableRoomIds.Contains(enrollment.ClassroomId));
        var inStudyRoomIds = timetableEnrollments
            .Where(enrollment =>
                IsCurrent(enrollment.ScheduleEntry!)
                && TeacherPresence.IsPresent(teacherAttendance[enrollment.ScheduleEntryId])
                && runnableRoomIds.Contains(enrollment.ClassroomId))
            .Select(enrollment => enrollment.ClassroomId)
            .ToHashSet();

        var rows = timetableEnrollments.Select(enrollment =>
        {
            var entry = enrollment.ScheduleEntry!;
            var isCurrent = IsCurrent(entry);
            var attendance = teacherAttendance[enrollment.ScheduleEntryId];
            var classroomStatus = enrollment.Classroom is null
                ? "Unavailable"
                : NormalizeClassroomStatus(enrollment.Classroom.Status);
            if (classroomStatus == "Available" && enrollment.Classroom?.DeviceOnline != true)
                classroomStatus = "Unavailable";
            var liveStatus = entry.DayOfWeek == now.DayOfWeek
                ? isCurrent
                    ? classroomStatus == "Available"
                        ? TeacherPresence.IsPresent(attendance) ? "Running" : "Available"
                        : classroomStatus
                    : entry.EndsAt <= time ? "Ended" : "Upcoming"
                : "Upcoming";
            return new WeeklyTimetableSlotDto(
                entry.Id,
                entry.TimetableCode,
                codeFormat.Derive(
                    string.IsNullOrWhiteSpace(enrollment.EnrollmentCode) ? entry.TimetableCode : enrollment.EnrollmentCode,
                    "timetable",
                    "operation"),
                entry.DayOfWeek.ToString(),
                entry.Shift,
                entry.StartsAt.ToString("HH:mm"),
                entry.EndsAt.ToString("HH:mm"),
                enrollment.Course?.Name ?? "—",
                enrollment.Teacher?.FullName ?? "—",
                enrollment.YearLevel,
                enrollment.Classroom?.ClassroomCode ?? "—",
                enrollment.Classroom?.RoomType ?? "Classroom",
                liveStatus,
                attendance,
                isCurrent && classroomStatus != "Available"
                    ? $"Classroom is {classroomStatus.ToLowerInvariant()} and cannot run."
                    : isCurrent ? TeacherPresence.Reason(attendance) : "Scheduled timetable period.");
        }).ToList();

        var periods = AcademicTimetablePolicy.All
            .Select(period => new TimetablePeriodDto(period.DayGroup, period.Session, period.StartsAt.ToString("HH:mm"), period.EndsAt.ToString("HH:mm")))
            .ToList();
        var learningSpaces = timetableEnrollments
            .Where(enrollment => enrollment.Classroom is not null)
            .GroupBy(enrollment => enrollment.ClassroomId)
            .Select(group => group.First())
            .OrderBy(enrollment => enrollment.Classroom!.ClassroomCode)
            .ToList();
        var roomRows = learningSpaces.Select(enrollment => new TimetableRoomDto(
            enrollment.ClassroomId,
            enrollment.Classroom!.ClassroomCode,
            codeFormat.Derive(enrollment.Classroom.ClassroomCode, "classroom", "operation"),
            enrollment.Classroom.RoomType,
            inStudyRoomIds.Contains(enrollment.ClassroomId)
                ? "Running"
                : NormalizeClassroomStatus(enrollment.Classroom.Status))).ToList();
        var metrics = new List<MetricDto>
        {
            new("Running", running.ToString(), "Classes right now", "green"),
            new("Available", available.ToString(), "Teacher absent or permission", "red"),
            new("Blocked", blocked.ToString(), "Classroom maintenance or unavailable", "amber"),
            new("Shifts", AcademicTimetablePolicy.Shifts.Count.ToString(), "Morning, afternoon, evening, weekend"),
            new("Rooms", learningSpaces.Count.ToString(), "Rooms used by matched Student and Timetable Enrollment", "violet")
        };
        return new OperationDto(
            Module,
            $"Weekly timetable · {context.Scope}",
            "Operational timetable rows come from matched Student Enrollment and Timetable Enrollment for the same academic year, semester, department, year, and shift.",
            metrics,
            context.Activity,
            context.Attention,
            WeeklySchedule: rows,
            TimetablePeriods: periods,
            TimetableRooms: roomRows);
    }

    private static string NormalizeClassroomStatus(string status) => status == "Reserved" ? "Maintenance" : status;
}
