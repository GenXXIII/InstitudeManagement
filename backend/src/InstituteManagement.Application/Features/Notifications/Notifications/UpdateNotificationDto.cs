namespace InstituteManagement.Application.Features.Notifications.Notifications;

public sealed record UpdateNotificationDto(string Title, string Message, string Severity, bool IsRead);
