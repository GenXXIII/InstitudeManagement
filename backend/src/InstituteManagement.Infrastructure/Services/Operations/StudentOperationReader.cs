using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
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
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(localNow);
        var shift = selection.Shift;
        var period = selection.Period;
        var selectedSchedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course)
            .Where(x => x.Status != "Cancelled"
                && x.DayOfWeek == selection.Date.DayOfWeek
                && x.StartsAt == period.StartsAt && x.EndsAt == period.EndsAt
                && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) selectedSchedules.Clear();
        var departmentIds = selectedSchedules.Select(x => x.Course!.DepartmentId).Distinct().ToList();
        var students = await db.Students.AsNoTracking().Include(x => x.Department)
            .Where(x => x.Status != "Inactive"
                && x.Shift == shift.Name
                && departmentIds.Contains(x.DepartmentId)
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .OrderBy(x => x.StudentCode)
            .ToListAsync(cancellationToken);
        var currentCohorts = selectedSchedules.Select(x => (x.Course!.DepartmentId, x.YearLevel)).ToHashSet();
        students = students.Where(x => currentCohorts.Contains((x.DepartmentId, x.YearLevel))).ToList();
        var ids = students.Select(x => x.Id).ToList();
        var attendance = selection.IsRunning
            ? await db.AttendanceRecords.AsNoTracking()
                .Where(x => ids.Contains(x.StudentId) && x.Date == selection.Date)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .ToListAsync(cancellationToken)
            : [];
        var status = attendance.GroupBy(x => x.StudentId)
            .ToDictionary(x => x.Key, x => NormalizeAttendance(x.First().Status));
        var defaultStatus = selection.IsRunning ? "Absent" : "Scheduled";
        var rows = students.Select(x => new StudentOperationDto(
                x.Id,
                x.FullName,
                x.StudentCode,
                x.Department?.Name ?? "—",
                x.YearLevel,
                x.Shift,
                status.GetValueOrDefault(x.Id, defaultStatus)))
            .OrderBy(x => AttendancePriority(x.AttendanceStatus))
            .ThenBy(x => x.StudentCode)
            .ToList();
        var metrics = new List<MetricDto>
        {
            new("Scheduled", rows.Count.ToString(), $"{shift.Name} · {selection.Date:dddd}"),
            new("Present", rows.Count(x => x.AttendanceStatus == "Present").ToString(), selection.IsRunning ? "Real-time" : "Not started", "green"),
            new("Permission", rows.Count(x => x.AttendanceStatus == "Permission").ToString(), selection.IsRunning ? "Real-time" : "Not started", "amber"),
            new("Absent", rows.Count(x => x.AttendanceStatus == "Absent").ToString(), selection.IsRunning ? "Real-time" : "Not started", "red")
        };
        var state = selection.IsRunning ? "currently in progress" : "next";
        return new OperationDto(
            Module,
            $"Student operations · {context.Scope}",
            $"Students assigned to the {state} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}).",
            metrics,
            context.Activity,
            context.Attention,
            Students: rows);
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
        "Scheduled" => 3,
        _ => 4
    };
}
