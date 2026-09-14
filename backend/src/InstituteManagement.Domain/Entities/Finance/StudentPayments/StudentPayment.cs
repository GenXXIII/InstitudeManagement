namespace InstituteManagement.Domain.Entities;

public sealed class StudentPayment : Entity
{
    public string PaymentCode { get; set; } = string.Empty;
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public string Currency { get; set; } = "USD";
    public DateOnly DueOn { get; set; }
    public string Status { get; set; } = "Pending";
    public string ConfirmationMethod { get; set; } = string.Empty;
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ReminderSentAtUtc { get; set; }
    public DateTime? ReminderReadAtUtc { get; set; }
}
