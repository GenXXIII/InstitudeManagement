namespace InstituteManagement.Domain.Entities;

public sealed class Department : Entity
{
    public required string DepartmentCode { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string Head { get; set; } = string.Empty;
    public Guid? HeadTeacherId { get; set; }
    public Teacher? HeadTeacher { get; set; }
    public bool IsActive { get; set; } = true;
}
