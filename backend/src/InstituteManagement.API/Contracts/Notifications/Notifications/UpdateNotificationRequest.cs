using InstituteManagement.Application.Features.Notifications.Notifications;

namespace InstituteManagement.API.Contracts.Notifications.Notifications;

public sealed record UpdateNotificationRequest(string Title, string Message, string Severity, bool IsRead)
{
    public UpdateNotificationDto ToDto() => new(Title, Message, Severity, IsRead);
}
