using InstituteManagement.Application.Common.Pagination;

namespace InstituteManagement.Application.Features.Notifications.Notifications;

public interface INotificationService
{
    Task<PagedResult<NotificationItemDto>> GetSystemAsync(string? search, bool unreadOnly, bool prioritizeUnread, PageRequest page, CancellationToken cancellationToken);
    Task<NotificationItemDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<NotificationItemDto> MarkReadAsync(Guid id, CancellationToken cancellationToken);
    Task<int> MarkAllReadAsync(CancellationToken cancellationToken);
    Task<NotificationItemDto> UpdateAsync(Guid id, UpdateNotificationDto request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
