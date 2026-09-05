namespace InstituteManagement.Domain.Entities;

public sealed class TeacherAssignment : Entity
{
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
}
