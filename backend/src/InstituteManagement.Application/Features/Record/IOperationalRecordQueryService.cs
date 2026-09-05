namespace InstituteManagement.Application.Features.Record;

public interface IOperationalRecordQueryService
{
    Task<IReadOnlyList<OperationalRecordDto>> GetAsync(string module, string? search, Guid? departmentId, bool history, CancellationToken cancellationToken);
}
