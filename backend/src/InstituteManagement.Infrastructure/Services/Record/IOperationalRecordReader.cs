using InstituteManagement.Application.DTOs;

namespace InstituteManagement.Infrastructure.Services.Record;

public interface IOperationalRecordReader
{
    string Module { get; }
    Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken);
}
