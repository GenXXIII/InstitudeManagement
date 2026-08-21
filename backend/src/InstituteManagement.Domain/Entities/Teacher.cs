namespace InstituteManagement.Domain.Entities;

public sealed class Teacher : Entity
{
    public required string TeacherNumber { get; set; }
    public required string FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhotoDataUrl { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Status { get; set; } = "Available";
}
