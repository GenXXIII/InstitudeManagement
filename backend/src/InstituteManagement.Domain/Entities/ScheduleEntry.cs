namespace InstituteManagement.Domain.Entities;

public sealed class ScheduleEntry : Entity
{
    public string TimetableCode { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public int YearLevel { get; set; } = 1;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string Status { get; set; } = "Upcoming";
}
