namespace InstituteManagement.Application.Features.Notifications.History;

public sealed record NotificationHistoryItemDto(
    Guid Id,
    string NotificationHistoryCode,
    Guid SourceId,
    string SourceCode,
    string Kind,
    string Type,
    string Title,
    string Message,
    string Action,
    DateTime CreateAt);
