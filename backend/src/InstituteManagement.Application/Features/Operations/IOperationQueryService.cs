namespace InstituteManagement.Application.Features.Operations;

public interface IOperationQueryService
{
    Task<OperationDto> GetAsync(string module, Guid? departmentId, CancellationToken cancellationToken);
}
