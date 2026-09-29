namespace InstituteManagement.Domain.Entities;

/// <summary>
/// The long-lived academic journey owned by Enrollment. Semester rows belong to
/// this aggregate and never replace the permanent Management student identity.
/// </summary>
public sealed class StudentAcademicEnrollment : Entity
{
    public string EnrollmentCode { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public ICollection<StudentEnrollment> Semesters { get; set; } = [];
}
