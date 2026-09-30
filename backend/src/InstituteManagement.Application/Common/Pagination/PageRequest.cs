namespace InstituteManagement.Application.Common.Pagination;

public readonly record struct PageRequest(int Page = 1, int PageSize = 40)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 40;
    public const int MaximumPageSize = 100;

    public int NormalizedPage => Page < DefaultPage ? DefaultPage : Page;

    public int NormalizedPageSize => PageSize <= 0
        ? DefaultPageSize
        : Math.Min(PageSize, MaximumPageSize);

    public int Skip => (int)Math.Min((long)(NormalizedPage - 1) * NormalizedPageSize, int.MaxValue);
}
