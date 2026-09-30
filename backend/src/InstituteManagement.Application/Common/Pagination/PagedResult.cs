namespace InstituteManagement.Application.Common.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, PageRequest request, int totalCount)
    {
        var pageSize = request.NormalizedPageSize;
        return new(
            items,
            request.NormalizedPage,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
