namespace InstituteManagement.Application.Features.Notifications.Notifications;

public sealed record NotificationItemDto(
    Guid Id,
    string NotificationCode,
    string Type,
    string Title,
    string Message,
    string Severity,
    bool IsRead,
    DateTime CreateAt);
