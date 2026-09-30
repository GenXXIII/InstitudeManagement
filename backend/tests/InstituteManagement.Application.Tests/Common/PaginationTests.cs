using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Tests.Common;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 0, 1, 40, 0)]
    [InlineData(-4, -8, 1, 40, 0)]
    [InlineData(2, 1000, 2, 100, 100)]
    [InlineData(3, 25, 3, 25, 50)]
    public void Page_request_normalizes_bounds(int page, int pageSize, int expectedPage, int expectedSize, int expectedSkip)
    {
        var request = new PageRequest(page, pageSize);

        Assert.Equal(expectedPage, request.NormalizedPage);
        Assert.Equal(expectedSize, request.NormalizedPageSize);
        Assert.Equal(expectedSkip, request.Skip);
    }

    [Fact]
    public void Page_request_does_not_overflow_for_extreme_page()
    {
        var request = new PageRequest(int.MaxValue, 100);

        Assert.Equal(int.MaxValue, request.Skip);
    }

    [Fact]
    public void Paged_result_reports_total_pages_from_normalized_size()
    {
        var result = PagedResult<int>.Create([1, 2], new PageRequest(2, 40), 81);

        Assert.Equal(2, result.Page);
        Assert.Equal(40, result.PageSize);
        Assert.Equal(81, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }
}
