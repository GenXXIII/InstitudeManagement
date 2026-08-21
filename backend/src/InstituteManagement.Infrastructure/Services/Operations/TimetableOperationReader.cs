using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TimetableOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "timetable";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Classroom).Where(x => x.Status != "Cancelled" && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId));
        var schedules = await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartsAt).ToListAsync(cancellationToken);
        var now = DateTime.Now; var time = TimeOnly.FromDateTime(now);
        var running = schedules.Count(x => x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time);
        var rooms = schedules.Select(x => x.ClassroomId).Distinct().Count();
        var rows = schedules.Select(x => new WeeklyTimetableSlotDto(x.Id, x.DayOfWeek.ToString(), x.StartsAt.ToString("HH:mm"), x.EndsAt.ToString("HH:mm"), x.Course?.Name ?? "—", x.Teacher?.FullName ?? "—", x.Classroom?.Code ?? "—", x.Status)).ToList();
        var metrics = new List<MetricDto> { new("Running", running.ToString(), "Right now", "green"), new("Weekdays", "5", "Monday to Friday"), new("Rooms", rooms.ToString(), "Scheduled facilities"), new("Total", schedules.Count.ToString(), "Weekly slots", "violet") };
        return new OperationDto(Module, $"Weekly timetable · {context.Scope}", "View the complete week and the course running at the current day and time.", metrics, context.Activity, context.Attention, WeeklySchedule: rows);
    }
}
