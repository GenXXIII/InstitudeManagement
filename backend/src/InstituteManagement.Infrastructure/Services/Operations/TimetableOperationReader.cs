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
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .Select(x => x.CourseId)
            .ToListAsync(cancellationToken);
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
        var classroomAssignments = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Classroom)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var learningSpaces = classroomAssignments.Where(x => x.Classroom is not null).Select(x => x.Classroom!).DistinctBy(x => x.Id).OrderBy(x => x.ClassroomCode).ToList();
        var assignmentByRoom = classroomAssignments.GroupBy(x => x.ClassroomId).ToDictionary(x => x.Key, x => x.First());
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var time = TimeOnly.FromDateTime(now);
        var running = schedules.Count(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time);
        var inStudyRoomIds = schedules
            .Where(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time)
            .Select(x => x.ClassroomId)
            .ToHashSet();
        var rows = schedules.Select(x =>
        {
            var period = AcademicTimetablePolicy.Find(x.DayOfWeek, x.StartsAt, x.EndsAt);
            var liveStatus = x.DayOfWeek == now.DayOfWeek
                ? x.StartsAt <= time && x.EndsAt > time ? "Running" : x.EndsAt <= time ? "Ended" : "Upcoming"
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
                liveStatus);
        }).ToList();
        var periods = AcademicTimetablePolicy.All
            .Select(x => new TimetablePeriodDto(x.DayGroup, x.Session, x.StartsAt.ToString("HH:mm"), x.EndsAt.ToString("HH:mm")))
            .ToList();
        var roomRows = learningSpaces
            .Select(room => new TimetableRoomDto(
                room.Id,
                room.ClassroomCode,
                room.RoomType,
                inStudyRoomIds.Contains(room.Id) && assignmentByRoom[room.Id].Status != "Unavailable" ? "In Study" : assignmentByRoom[room.Id].Status))
            .ToList();
        var metrics = new List<MetricDto>
        {
            new("Running", running.ToString(), "Classes right now", "green"),
            new("Shifts", AcademicTimetablePolicy.Shifts.Count.ToString(), "Morning, afternoon, evening, weekend"),
            new("Concurrent", "4", "One class for each year"),
            new("Rooms", learningSpaces.Count.ToString(), "Enrollment-assigned learning spaces", "violet")
        };
        return new OperationDto(
            Module,
            $"Weekly timetable · {context.Scope}",
            "Morning, Afternoon, and Evening run Monday through Friday with two periods per day; Weekend runs five periods on Saturday and Sunday.",
            metrics,
            context.Activity,
            context.Attention,
            WeeklySchedule: rows,
            TimetablePeriods: periods,
            TimetableRooms: roomRows);
    }
}
