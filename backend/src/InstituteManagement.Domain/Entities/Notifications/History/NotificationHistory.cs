namespace InstituteManagement.Domain.Entities;

public sealed class NotificationHistory : Entity
{
    public string NotificationHistoryCode { get; set; } = $"NHS-{Guid.NewGuid():N}";
    public Guid SourceId { get; set; }
    public string SourceCode { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
