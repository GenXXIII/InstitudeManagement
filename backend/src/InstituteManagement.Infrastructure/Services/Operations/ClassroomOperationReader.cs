using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class ClassroomOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "classrooms";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive");
        var classrooms = await query.OrderBy(x => x.ClassroomCode).ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var roomIds = classrooms.Select(room => room.Id).ToList();
        var timetableRoomIds = (await db.ScheduleEntries.AsNoTracking()
            .Where(entry => roomIds.Contains(entry.ClassroomId)
                && entry.Status != "Cancelled"
                && entry.DayOfWeek == selection.Date.DayOfWeek
                && entry.StartsAt == period.StartsAt && entry.EndsAt == period.EndsAt)
            .Select(entry => entry.ClassroomId)
            .ToListAsync(cancellationToken)).ToHashSet();
        if (!selection.IsRunning) timetableRoomIds.Clear();
        var rows = classrooms
            .Select(x => new ClassroomOperationDto(x.Id, x.ClassroomCode, x.RoomType, char.IsDigit(x.ClassroomCode.FirstOrDefault()) ? x.ClassroomCode[0] - '0' : 1, x.Building, x.Capacity, x.DeviceOnline ? "Online" : "Offline", timetableRoomIds.Contains(x.Id) && x.Status is not ("Offline" or "Starting") ? "In Study" : x.Status))
            .OrderBy(x => x.Room)
            .ToList();
        var metrics = new List<MetricDto> { new("Total", classrooms.Count.ToString(), "Classrooms and meeting rooms"), new("In Study", timetableRoomIds.Count.ToString(), "Learning now", "green"), new("Available", classrooms.Count(x => x.Status == "Available" && !timetableRoomIds.Contains(x.Id)).ToString(), "Ready"), new("Offline", classrooms.Count(x => !x.DeviceOnline || x.Status == "Offline").ToString(), "Needs attention", "red") };
        var timing = selection.IsRunning ? "current" : "next";
        return new OperationDto(Module, $"Learning-space operations · {context.Scope}", $"Room status for the {timing} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}).", metrics, context.Activity, context.Attention, Classrooms: rows);
    }
}
