using InstituteManagement.Application.Features.Results;

using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Results;

public interface IResultQueryService
{
    Task<PagedResult<SemesterResultDto>> GetAsync(Guid? departmentId, int? year, string? semester, string? academicYear, bool history, Guid? studentId, bool publishedOnly, string? search, string? outcome, PageRequest page, CancellationToken cancellationToken);
    Task<ResultPublicationReadinessDto> GetPublicationReadinessAsync(CancellationToken cancellationToken);
    Task<int> PublishAllAsync(CancellationToken cancellationToken);
}
