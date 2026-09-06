using InstituteManagement.Application.Features.Notifications.Announcements;

namespace InstituteManagement.API.Contracts.Notifications.Announcements;

public sealed record AnnouncementRequest(string Type, string Title, string Message)
{
    public AnnouncementRequestDto ToDto() => new(Type, Title, Message);
}
