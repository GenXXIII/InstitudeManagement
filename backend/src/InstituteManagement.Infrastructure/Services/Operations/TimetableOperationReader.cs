using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TimetableOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "timetable";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Classroom).Where(x => x.Status != "Cancelled" && x.Status != "Completed" && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId));
        var schedules = await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartsAt).ToListAsync(cancellationToken);
        var learningSpaces = await db.Classrooms.AsNoTracking()
            .Where(room => room.Status != "Inactive")
            .OrderBy(room => room.ClassroomCode)
            .ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken); var time = TimeOnly.FromDateTime(now);
        schedules = schedules.Where(x => x.DayOfWeek != now.DayOfWeek || x.EndsAt > time).ToList();
        var running = schedules.Count(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time);
        var inStudyRoomIds = schedules.Where(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time).Select(x => x.ClassroomId).ToHashSet();
        var rows = schedules.Select(x =>
        {
            var period = AcademicTimetablePolicy.Find(x.DayOfWeek, x.StartsAt, x.EndsAt);
            var liveStatus = x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time ? "Running" : "Upcoming";
            return new WeeklyTimetableSlotDto(x.Id, x.DayOfWeek.ToString(), period?.Session ?? "Custom", x.StartsAt.ToString("HH:mm"), x.EndsAt.ToString("HH:mm"), x.Course?.Name ?? "—", x.Teacher?.FullName ?? "—", x.YearLevel, x.Classroom?.ClassroomCode ?? "—", x.Classroom?.RoomType ?? "Classroom", liveStatus);
        }).ToList();
        var periods = AcademicTimetablePolicy.All.Select(x => new TimetablePeriodDto(x.DayGroup, x.Session, x.StartsAt.ToString("HH:mm"), x.EndsAt.ToString("HH:mm"))).ToList();
        var roomRows = learningSpaces.Select(room => new TimetableRoomDto(room.Id, room.ClassroomCode, room.RoomType, inStudyRoomIds.Contains(room.Id) && room.Status is not ("Offline" or "Starting") ? "In Study" : room.Status)).ToList();
        var metrics = new List<MetricDto> { new("Running", running.ToString(), "Classes right now", "green"), new("Years", "4", "Year 1 to Year 4"), new("Concurrent", "4", "Classes in every period"), new("Rooms", learningSpaces.Count.ToString(), "Available learning spaces", "violet") };
        return new OperationDto(Module, $"Weekly timetable · {context.Scope}", "Every teaching period includes one class for each of Year 1, Year 2, Year 3, and Year 4.", metrics, context.Activity, context.Attention, WeeklySchedule: rows, TimetablePeriods: periods, TimetableRooms: roomRows);
    }
}
