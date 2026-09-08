namespace InstituteManagement.Domain.Entities;

public sealed class TimetableEnrollment : Entity
{
    public string EnrollmentCode { get; set; } = string.Empty;
    public Guid ScheduleEntryId { get; set; }
    public ScheduleEntry? ScheduleEntry { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public Guid ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public int YearLevel { get; set; } = 1;
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
