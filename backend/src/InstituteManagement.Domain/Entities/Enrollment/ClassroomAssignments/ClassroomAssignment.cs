namespace InstituteManagement.Domain.Entities;

public sealed class ClassroomAssignment : Entity
{
    public string EnrollmentCode { get; set; } = string.Empty;
    public Guid ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int Capacity { get; set; } = 40;
    public string Access { get; set; } = "Shared institute";
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
}
