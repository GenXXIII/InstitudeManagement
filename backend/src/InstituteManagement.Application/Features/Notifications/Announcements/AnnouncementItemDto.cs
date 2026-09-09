namespace InstituteManagement.Application.Features.Notifications.Announcements;

public sealed record AnnouncementItemDto(
    Guid Id,
    string AnnouncementCode,
    Guid? NotificationId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreateAt);
