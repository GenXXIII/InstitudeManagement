namespace InstituteManagement.Domain.Entities;

/// <summary>
/// Relational, immutable-after-semester-completion attendance evidence for one
/// student in one recorded class session. Names and codes are deliberate
/// historical snapshots; identity and semester references remain constrained.
/// </summary>
public sealed class ClassSessionStudentAttendance : Entity
{
    public Guid ClassSessionRecordId { get; set; }
    public ClassSessionRecord? ClassSessionRecord { get; set; }
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CheckedInAt { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}
