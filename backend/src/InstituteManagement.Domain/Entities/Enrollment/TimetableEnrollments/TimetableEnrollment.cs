namespace InstituteManagement.Domain.Entities;

public sealed class TimetableEnrollment : Entity
{
    public Guid ScheduleEntryId { get; set; }
    public ScheduleEntry? ScheduleEntry { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
