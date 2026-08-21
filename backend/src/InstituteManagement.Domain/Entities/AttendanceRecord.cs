namespace InstituteManagement.Domain.Entities;

public sealed class AttendanceRecord : Entity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? CheckedInAt { get; set; }
    public string Status { get; set; } = "Present";
    public string Method { get; set; } = "ID Card";
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = "Semester 1";
}
