namespace InstituteManagement.Application.Features.Notifications.History;

public interface INotificationHistoryService
{
    Task<IReadOnlyList<NotificationHistoryItemDto>> GetAsync(CancellationToken cancellationToken);
    Task<NotificationHistoryItemDto> GetAsync(string codeOrId, CancellationToken cancellationToken);
}
