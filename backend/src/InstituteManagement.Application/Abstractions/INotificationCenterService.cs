using InstituteManagement.Application.DTOs.Notifications;

namespace InstituteManagement.Application.Abstractions;

public interface INotificationCenterService
{
    Task<IReadOnlyList<NotificationItemDto>> GetNotificationsAsync(CancellationToken cancellationToken);
    Task<NotificationItemDto> GetNotificationAsync(Guid id, CancellationToken cancellationToken);
    Task<NotificationItemDto> MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken);
    Task<NotificationItemDto> UpdateNotificationAsync(Guid id, UpdateNotificationRequestDto request, CancellationToken cancellationToken);
    Task DeleteNotificationAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AnnouncementItemDto>> GetAnnouncementsAsync(CancellationToken cancellationToken);
    Task<AnnouncementItemDto> CreateAnnouncementAsync(AnnouncementRequestDto request, CancellationToken cancellationToken);
    Task<AnnouncementItemDto> UpdateAnnouncementAsync(Guid id, AnnouncementRequestDto request, CancellationToken cancellationToken);
    Task DeleteAnnouncementAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationHistoryItemDto>> GetHistoryAsync(CancellationToken cancellationToken);
    Task<NotificationHistoryItemDto> GetHistoryItemAsync(Guid id, CancellationToken cancellationToken);
}
