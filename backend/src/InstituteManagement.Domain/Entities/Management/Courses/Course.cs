namespace InstituteManagement.Domain.Entities;

public sealed class Course : Entity
{
    public required string CourseCode { get; set; }
    public required string Name { get; set; }
    public int YearLevel { get; set; } = 1;
    public string Semester { get; set; } = "Semester 1";
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public int Capacity { get; set; } = 40;
    public bool IsActive { get; set; } = true;
}
