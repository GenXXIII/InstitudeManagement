namespace InstituteManagement.Domain.Entities;

public sealed class FinancialPayment : Entity
{
    public string PaymentCode { get; set; } = string.Empty;
    public Guid FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public string TransactionReference { get; set; } = string.Empty;
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}
