using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class StudentOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "students";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var time = TimeOnly.FromDateTime(localNow);
        var currentSchedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course)
            .Where(x => x.Status != "Cancelled" && x.Status != "Completed" && x.DayOfWeek == localNow.DayOfWeek && x.StartsAt <= time && x.EndsAt > time && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var departmentIds = currentSchedules.Select(x => x.Course!.DepartmentId).Distinct().ToList();
        var students = await db.Students.AsNoTracking().Include(x => x.Department)
            .Where(x => x.Status != "Inactive" && departmentIds.Contains(x.DepartmentId) && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .OrderBy(x => x.StudentCode)
            .ToListAsync(cancellationToken);
        var currentCohorts = currentSchedules.Select(x => (x.Course!.DepartmentId, x.YearLevel, Shift: Session(x.StartsAt))).ToHashSet();
        students = students.Where(x => currentCohorts.Contains((x.DepartmentId, x.YearLevel, x.Shift))).ToList();
        var ids = students.Select(x => x.Id).ToList();
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => ids.Contains(x.StudentId) && x.Date == DateOnly.FromDateTime(localNow)).OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);
        var status = attendance.GroupBy(x => x.StudentId).ToDictionary(x => x.Key, x => NormalizeAttendance(x.First().Status));
        var rows = students.Select(x => new StudentOperationDto(x.Id, x.FullName, x.StudentCode, x.Department?.Name ?? "—", x.YearLevel, x.Shift, status.GetValueOrDefault(x.Id, "Absent")))
            .OrderBy(x => AttendancePriority(x.AttendanceStatus))
            .ThenBy(x => x.StudentCode)
            .ToList();
        var metrics = new List<MetricDto> { new("Scheduled", rows.Count.ToString(), "Current timetable period"), new("Present", rows.Count(x => x.AttendanceStatus == "Present").ToString(), "Real-time", "green"), new("Permission", rows.Count(x => x.AttendanceStatus == "Permission").ToString(), "Real-time", "amber"), new("Absent", rows.Count(x => x.AttendanceStatus == "Absent").ToString(), "Real-time", "red") };
        return new OperationDto(Module, $"Student operations · {context.Scope}", "Students assigned to the timetable period currently in progress, ordered as Present, Permission, then Absent.", metrics, context.Activity, context.Attention, Students: rows);
    }

    private static string NormalizeAttendance(string status) => status switch
    {
        "Present" or "Late" => "Present",
        "Excused" or "Permission" => "Permission",
        _ => "Absent"
    };

    private static int AttendancePriority(string status) => status switch
    {
        "Present" => 0,
        "Permission" => 1,
        "Absent" => 2,
        _ => 3
    };

    private static string Session(TimeOnly startsAt) => startsAt.Hour >= 17 ? "Evening" : startsAt.Hour >= 13 ? "Afternoon" : "Morning";
}
