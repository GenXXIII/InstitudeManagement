using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class TeacherOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "teachers";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.Teachers.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var teachers = await query.OrderBy(x => x.FullName).Take(12).ToListAsync(cancellationToken);
        var rows = teachers.Select(x => new TeacherOperationDto(x.Id, x.FullName, x.TeacherNumber, x.Department?.Name ?? "—", x.Status)).ToList();
        var metrics = new List<MetricDto> { new("Total", (await query.CountAsync(cancellationToken)).ToString(), "Active faculty"), new("Teaching", (await query.CountAsync(x => x.Status == "Teaching", cancellationToken)).ToString(), "Right now", "green"), new("Available", (await query.CountAsync(x => x.Status == "Available", cancellationToken)).ToString(), "On campus"), new("On leave", (await query.CountAsync(x => x.Status == "On leave", cancellationToken)).ToString(), "Today", "amber") };
        return new OperationDto(Module, $"Teacher operations · {context.Scope}", "Monitor faculty availability and teaching assignments for the selected department.", metrics, context.Activity, context.Attention, Teachers: rows);
    }
}
