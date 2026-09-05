namespace InstituteManagement.Domain.Entities;

public sealed class StudentEnrollment : Entity
{
    public string EnrollmentCode { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int YearLevel { get; set; } = 1;
    public string Shift { get; set; } = "Morning";
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
