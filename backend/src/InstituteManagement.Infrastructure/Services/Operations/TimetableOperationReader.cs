using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TimetableOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "timetable";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var enrolledCourseAssignments = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var courseAssignments = enrolledCourseAssignments.Select(x => x.CourseId).ToList();
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var query = db.ScheduleEntries.AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Teacher)
            .Include(x => x.Classroom)
            .Where(x => x.Status != "Cancelled" && enrolledTimetableIds.Contains(x.Id) && courseAssignments.Contains(x.CourseId));
        var schedules = await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartsAt).ToListAsync(cancellationToken);
        var teacherIds = schedules.Select(x => x.TeacherId).Distinct().ToList();
        var teacherAssignments = await db.TeacherAssignments.AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId) && x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned")
            .ToListAsync(cancellationToken);
        var teacherAttendance = schedules.ToDictionary(schedule => schedule.Id, schedule =>
        {
            var department = enrolledCourseAssignments.First(item => item.CourseId == schedule.CourseId).DepartmentId;
            var assignment = teacherAssignments
                .Where(item => item.TeacherId == schedule.TeacherId && (item.DepartmentId == department || item.DepartmentId == null))
                .OrderByDescending(item => item.DepartmentId == department)
                .FirstOrDefault();
            return TeacherPresence.Attendance(schedule.Teacher?.Status, assignment?.Status);
        });
        var classroomAssignments = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Classroom)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var learningSpaces = classroomAssignments.Where(x => x.Classroom is not null).Select(x => x.Classroom!).DistinctBy(x => x.Id).OrderBy(x => x.ClassroomCode).ToList();
        var assignmentByRoom = classroomAssignments.GroupBy(x => x.ClassroomId).ToDictionary(x => x.Key, x => x.First());
        var runnableRoomIds = classroomAssignments
            .Where(x => NormalizeClassroomStatus(x.Status) == "Available" && x.Classroom?.DeviceOnline == true)
            .Select(x => x.ClassroomId)
            .ToHashSet();
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var time = TimeOnly.FromDateTime(now);
        var running = schedules.Count(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time
            && TeacherPresence.IsPresent(teacherAttendance[x.Id]) && runnableRoomIds.Contains(x.ClassroomId));
        var notRunning = schedules.Count(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time
            && (!TeacherPresence.IsPresent(teacherAttendance[x.Id]) || !runnableRoomIds.Contains(x.ClassroomId)));
        var inStudyRoomIds = schedules
            .Where(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time
                && TeacherPresence.IsPresent(teacherAttendance[x.Id]) && runnableRoomIds.Contains(x.ClassroomId))
            .Select(x => x.ClassroomId)
            .ToHashSet();
        var rows = schedules.Select(x =>
        {
            var period = AcademicTimetablePolicy.Find(x.DayOfWeek, x.StartsAt, x.EndsAt);
            var isCurrent = x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time;
            var attendance = teacherAttendance[x.Id];
            var classroomStatus = assignmentByRoom.TryGetValue(x.ClassroomId, out var classroomAssignment)
                ? NormalizeClassroomStatus(classroomAssignment.Status)
                : "Unavailable";
            if (classroomStatus == "Available" && x.Classroom?.DeviceOnline != true) classroomStatus = "Unavailable";
            var liveStatus = x.DayOfWeek == now.DayOfWeek
                ? isCurrent ? classroomStatus == "Available" ? TeacherPresence.SessionStatus(attendance) : classroomStatus : x.EndsAt <= time ? "Ended" : "Upcoming"
                : "Upcoming";
            return new WeeklyTimetableSlotDto(
                x.Id,
                x.TimetableCode,
                x.DayOfWeek.ToString(),
                period?.Session ?? "Custom",
                x.StartsAt.ToString("HH:mm"),
                x.EndsAt.ToString("HH:mm"),
                x.Course?.Name ?? "—",
                x.Teacher?.FullName ?? "—",
                x.YearLevel,
                x.Classroom?.ClassroomCode ?? "—",
                x.Classroom?.RoomType ?? "Classroom",
                liveStatus,
                attendance,
                isCurrent && classroomStatus != "Available" ? $"Classroom is {classroomStatus.ToLowerInvariant()} and cannot run." : isCurrent ? TeacherPresence.Reason(attendance) : "Scheduled timetable period.");
        }).ToList();
        var periods = AcademicTimetablePolicy.All
            .Select(x => new TimetablePeriodDto(x.DayGroup, x.Session, x.StartsAt.ToString("HH:mm"), x.EndsAt.ToString("HH:mm")))
            .ToList();
        var roomRows = learningSpaces
            .Select(room => new TimetableRoomDto(
                room.Id,
                room.ClassroomCode,
                room.RoomType,
                inStudyRoomIds.Contains(room.Id) ? "In Study" : NormalizeClassroomStatus(assignmentByRoom[room.Id].Status)))
            .ToList();
        var metrics = new List<MetricDto>
        {
            new("Running", running.ToString(), "Classes right now", "green"),
            new("Not running", notRunning.ToString(), "Teacher absent or on permission", "red"),
            new("Shifts", AcademicTimetablePolicy.Shifts.Count.ToString(), "Morning, afternoon, evening, weekend"),
            new("Concurrent", "4", "One class for each year"),
            new("Rooms", learningSpaces.Count.ToString(), "Enrollment-assigned learning spaces", "violet")
        };
        return new OperationDto(
            Module,
            $"Weekly timetable · {context.Scope}",
            "A timetable period runs only when its assigned teacher is present and its classroom is Available. Maintenance and Unavailable classroom states stay fixed until changed in Enrollment.",
            metrics,
            context.Activity,
            context.Attention,
            WeeklySchedule: rows,
            TimetablePeriods: periods,
            TimetableRooms: roomRows);
    }

    private static string NormalizeClassroomStatus(string status) => status == "Reserved" ? "Maintenance" : status;
}
