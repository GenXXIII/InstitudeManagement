using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Record;

namespace InstituteManagement.Application.Features.History;

public sealed record HistoryPageDto(
    IReadOnlyList<RecordDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int TotalEvents)
{
    public static HistoryPageDto Create(IReadOnlyList<RecordDto> items, PageRequest request, int totalCount, int totalEvents)
    {
        var pageSize = request.NormalizedPageSize;
        return new(
            items,
            request.NormalizedPage,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            totalEvents);
    }
}
