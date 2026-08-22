using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class StudentOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "students";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.Students.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var students = await query.OrderBy(x => x.FullName).Take(12).ToListAsync(cancellationToken);
        var ids = students.Select(x => x.Id).ToList();
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => ids.Contains(x.StudentId) && x.Date == DateOnly.FromDateTime(DateTime.UtcNow)).OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);
        var status = attendance.GroupBy(x => x.StudentId).ToDictionary(x => x.Key, x => x.First().Status);
        var present = attendance.Count(x => x.Status is "Present" or "Late");
        var rows = students.Select(x => new StudentOperationDto(x.Id, x.FullName, x.StudentCode, x.Department?.Name ?? "—", x.YearLevel, status.GetValueOrDefault(x.Id, "Absent"))).ToList();
        var metrics = new List<MetricDto> { new("Total", (await query.CountAsync(cancellationToken)).ToString(), "Enrolled students"), new("Present", attendance.Count(x => x.Status == "Present").ToString(), "Today", "green"), new("Absent", Math.Max(0, await query.CountAsync(cancellationToken) - present).ToString(), "Not checked in", "red"), new("Late", attendance.Count(x => x.Status == "Late").ToString(), "Today", "amber") };
        return new OperationDto(Module, $"Student operations · {context.Scope}", "See live enrollment and attendance state for the selected department.", metrics, context.Activity, context.Attention, Students: rows);
    }
}
