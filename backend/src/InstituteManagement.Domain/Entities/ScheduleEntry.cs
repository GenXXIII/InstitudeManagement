namespace InstituteManagement.Domain.Entities;

public sealed class ScheduleEntry : Entity
{
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string Status { get; set; } = "Upcoming";
}
