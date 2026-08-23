using InstituteManagement.Application.DTOs;
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
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken); var time = TimeOnly.FromDateTime(now);
        var roomIds = classrooms.Select(room => room.Id).ToList();
        var inStudyRoomIds = (await db.ScheduleEntries.AsNoTracking()
            .Where(entry => roomIds.Contains(entry.ClassroomId) && entry.Status != "Cancelled" && entry.DayOfWeek == now.DayOfWeek && entry.StartsAt <= time && entry.EndsAt > time)
            .Select(entry => entry.ClassroomId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var rows = classrooms.Select(x => new ClassroomOperationDto(x.Id, x.ClassroomCode, x.RoomType, char.IsDigit(x.ClassroomCode.FirstOrDefault()) ? x.ClassroomCode[0] - '0' : 1, x.Building, x.Capacity, x.DeviceOnline ? "Online" : "Offline", inStudyRoomIds.Contains(x.Id) && x.Status is not ("Offline" or "Starting") ? "In Study" : x.Status)).ToList();
        var metrics = new List<MetricDto> { new("Total", classrooms.Count.ToString(), "Classrooms and meeting rooms"), new("In Study", inStudyRoomIds.Count.ToString(), "Learning now", "green"), new("Available", classrooms.Count(x => x.Status == "Available" && !inStudyRoomIds.Contains(x.Id)).ToString(), "Ready"), new("Offline", classrooms.Count(x => !x.DeviceOnline || x.Status == "Offline").ToString(), "Needs attention", "red") };
        return new OperationDto(Module, $"Learning-space operations · {context.Scope}", "Live study status for institute-shared classrooms and meeting rooms available to every department.", metrics, context.Activity, context.Attention, Classrooms: rows);
    }
}
