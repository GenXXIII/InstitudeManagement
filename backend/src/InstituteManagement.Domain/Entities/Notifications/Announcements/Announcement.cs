namespace InstituteManagement.Domain.Entities;

public sealed class Announcement : Entity
{
    public string AnnouncementCode { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? NotificationId { get; set; }
    public Notification? Notification { get; set; }
}
