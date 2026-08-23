using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TeacherOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "teachers";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken); var time = TimeOnly.FromDateTime(now);
        var currentTeacherIds = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.Status != "Cancelled" && x.Status != "Completed" && x.DayOfWeek == now.DayOfWeek && x.StartsAt <= time && x.EndsAt > time && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .Select(x => x.TeacherId)
            .ToHashSetAsync(cancellationToken);
        var teachers = await db.Teachers.AsNoTracking().Include(x => x.Department).Where(x => currentTeacherIds.Contains(x.Id)).OrderBy(x => x.TeacherCode).ToListAsync(cancellationToken);
        var rows = teachers.Select(x => new TeacherOperationDto(x.Id, x.FullName, x.TeacherCode, x.Department?.Name ?? "—", TeacherAttendance(x.Status)))
            .OrderBy(x => AttendancePriority(x.Status))
            .ThenBy(x => x.TeacherCode)
            .ToList();
        var metrics = new List<MetricDto> { new("Scheduled", rows.Count.ToString(), "Current timetable period"), new("Present", rows.Count(x => x.Status == "Present").ToString(), "Teaching now", "green"), new("Permission", rows.Count(x => x.Status == "Permission").ToString(), "Approved absence", "amber"), new("Absent", rows.Count(x => x.Status == "Absent").ToString(), "Not present", "red") };
        return new OperationDto(Module, $"Teacher operations · {context.Scope}", "Teachers assigned to the timetable period currently in progress, ordered by attendance.", metrics, context.Activity, context.Attention, Teachers: rows);
    }

    private static string TeacherAttendance(string status) => status switch { "On leave" => "Permission", "Inactive" => "Absent", _ => "Present" };
    private static int AttendancePriority(string status) => status switch { "Present" => 0, "Permission" => 1, "Absent" => 2, _ => 3 };
}
