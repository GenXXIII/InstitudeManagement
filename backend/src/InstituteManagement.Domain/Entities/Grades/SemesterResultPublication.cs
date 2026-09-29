namespace InstituteManagement.Domain.Entities;

public sealed class SemesterResultPublication : Entity
{
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = "Semester 1";
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
}
