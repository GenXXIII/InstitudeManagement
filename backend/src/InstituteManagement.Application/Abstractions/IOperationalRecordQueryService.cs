using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Application.Abstractions;

public interface IOperationalRecordQueryService
{
    Task<IReadOnlyList<OperationalRecordDto>> GetAsync(string module, string? search, Guid? departmentId, CancellationToken cancellationToken);
}
