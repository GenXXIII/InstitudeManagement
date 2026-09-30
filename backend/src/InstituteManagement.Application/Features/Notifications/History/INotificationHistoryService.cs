using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Notifications.History;

public interface INotificationHistoryService
{
    Task<PagedResult<NotificationHistoryItemDto>> GetAsync(string? search, PageRequest page, CancellationToken cancellationToken);
    Task<NotificationHistoryItemDto> GetAsync(string codeOrId, CancellationToken cancellationToken);
}
