namespace InstituteManagement.Application.DTOs.Notifications;

public sealed record NotificationItemDto(Guid Id, string NotificationCode, string Type, string Title, string Message, string Severity, bool IsRead, DateTime CreateAt);
public sealed record AnnouncementItemDto(Guid Id, string AnnouncementCode, Guid? NotificationId, string Type, string Title, string Message, DateTime CreateAt);
public sealed record NotificationHistoryItemDto(Guid Id, string NotificationHistoryCode, Guid SourceId, string SourceCode, string Kind, string Type, string Title, string Message, string Action, DateTime CreateAt);
public sealed record UpdateNotificationRequestDto(string Title, string Message, string Severity, bool IsRead);
public sealed record AnnouncementRequestDto(string Type, string Title, string Message);
