namespace InstituteManagement.Domain.Entities;

public sealed class Student : Entity
{
    public required string StudentCode { get; set; }
    public required string FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhotoDataUrl { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int YearLevel { get; set; }
    public string Shift { get; set; } = "Morning";
    public string Status { get; set; } = "Active";
}
