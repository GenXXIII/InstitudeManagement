using InstituteManagement.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Common;

internal static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> orderedQuery,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var totalCount = await orderedQuery.CountAsync(cancellationToken);
        var items = await orderedQuery
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);
        return PagedResult<T>.Create(items, request, totalCount);
    }
}
