namespace InstituteManagement.Domain.Entities;

public sealed class AuditLog : Entity
{
    public Guid? ResourceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
