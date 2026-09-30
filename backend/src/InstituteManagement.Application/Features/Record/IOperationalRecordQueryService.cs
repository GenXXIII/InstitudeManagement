using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Record;

public interface IOperationalRecordQueryService
{
    Task<PagedResult<OperationalRecordDto>> GetAsync(string module, string? search, Guid? departmentId, int? year, string? period, bool history, PageRequest page, CancellationToken cancellationToken);
}
