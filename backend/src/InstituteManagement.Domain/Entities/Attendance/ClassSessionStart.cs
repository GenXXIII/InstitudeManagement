namespace InstituteManagement.Domain.Entities;

public sealed class ClassSessionStart : Entity
{
    public Guid ScheduleEntryId { get; set; }
    public ScheduleEntry? ScheduleEntry { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
}
