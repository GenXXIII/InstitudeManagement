namespace InstituteManagement.Domain.Entities;

public sealed class TeacherAssignment : Entity
{
    public string EnrollmentCode { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string OperationCode { get; set; } = string.Empty;
    public string RecordCode { get; set; } = string.Empty;
    public string HistoryCode { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
}
