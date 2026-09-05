namespace InstituteManagement.Domain.Entities;

public sealed class Notification : Entity
{
    public string NotificationCode { get; set; } = $"NOT-{Guid.NewGuid():N}";
    public string Type { get; set; } = "System";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public bool IsRead { get; set; }
}
