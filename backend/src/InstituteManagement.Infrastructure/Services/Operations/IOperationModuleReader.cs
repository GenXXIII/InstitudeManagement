using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Infrastructure.Services.Operations;

public interface IOperationModuleReader
{
    string Module { get; }
    Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken);
}
