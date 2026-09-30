using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.History;

public interface IHistoryQueryService
{
    Task<HistoryPageDto> GetAsync(string? search, string? type, PageRequest page, CancellationToken cancellationToken);
}
