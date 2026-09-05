namespace InstituteManagement.Domain.Entities;

public sealed class CourseAssignment : Entity
{
    public string EnrollmentCode { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public int YearLevel { get; set; } = 1;
    public int Capacity { get; set; } = 40;
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
