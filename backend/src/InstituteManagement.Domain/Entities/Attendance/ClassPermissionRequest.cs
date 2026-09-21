namespace InstituteManagement.Domain.Entities;

public sealed class ClassPermissionRequest : Entity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public DateOnly SessionDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}
