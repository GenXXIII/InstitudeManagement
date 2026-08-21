using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface IOperationQueryService
{
    Task<OperationDto> GetAsync(string module, Guid? departmentId, CancellationToken cancellationToken);
}
