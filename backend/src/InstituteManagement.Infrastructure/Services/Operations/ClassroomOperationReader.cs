using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class ClassroomOperationReader(InstituteDbContext db, OperationContextService contextService) : IOperationModuleReader
{
    public string Module => "classrooms";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var query = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId));
        var classrooms = await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var rows = classrooms.Select(x => new ClassroomOperationDto(x.Id, x.Code, char.IsDigit(x.Code.FirstOrDefault()) ? x.Code[0] - '0' : 1, x.Building, x.Capacity, x.DeviceOnline ? "Online" : "Offline", x.Status)).ToList();
        var metrics = new List<MetricDto> { new("Total", classrooms.Count.ToString(), "Active rooms"), new("Running", classrooms.Count(x => x.Status == "Running").ToString(), "In session", "green"), new("Available", classrooms.Count(x => x.Status == "Available").ToString(), "Ready"), new("Offline", classrooms.Count(x => !x.DeviceOnline).ToString(), "Needs attention", "red") };
        return new OperationDto(Module, $"Classroom operations · {context.Scope}", "Five-floor classroom and device overview for the selected department.", metrics, context.Activity, context.Attention, Classrooms: rows);
    }
}
