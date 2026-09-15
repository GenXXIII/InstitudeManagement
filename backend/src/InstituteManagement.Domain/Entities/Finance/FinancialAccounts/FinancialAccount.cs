namespace InstituteManagement.Domain.Entities;

public sealed class FinancialAccount : Entity
{
    public string FinancialAccountCode { get; set; } = string.Empty;
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment? StudentEnrollment { get; set; }
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public decimal TuitionFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string AdjustmentReason { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public DateOnly DueOn { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? ReminderSentAtUtc { get; set; }
    public DateTime? ReminderReadAtUtc { get; set; }
    public ICollection<FinancialPayment> Payments { get; set; } = [];
}
