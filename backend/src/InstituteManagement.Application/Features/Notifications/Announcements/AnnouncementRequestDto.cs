namespace InstituteManagement.Application.Features.Notifications.Announcements;

public sealed record AnnouncementRequestDto(string AnnouncementCode, string Type, string Title, string Message);
