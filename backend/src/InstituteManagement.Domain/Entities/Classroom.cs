namespace InstituteManagement.Domain.Entities;

public sealed class Classroom : Entity
{
    public required string ClassroomCode { get; set; }
    public string Building { get; set; } = string.Empty;
    public string RoomType { get; set; } = "Classroom";
    public int Capacity { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Status { get; set; } = "Available";
    public bool DeviceOnline { get; set; } = true;
}
