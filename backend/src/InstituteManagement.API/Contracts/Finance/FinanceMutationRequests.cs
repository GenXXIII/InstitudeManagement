using InstituteManagement.Application.Features.Finance;

namespace InstituteManagement.API.Contracts.Finance;

public sealed record FinanceDeclarationRequest(
    string? Title,
    string? PaymentPlan,
    decimal Amount,
    DateOnly DueOn,
    DateTime ExpiresAtUtc)
{
    public FinanceDeclarationDto ToDto() => new(Title, PaymentPlan, Amount, DueOn, ExpiresAtUtc);
}

public sealed record RecordFinancePaymentRequest(
    decimal Amount,
    string? Method,
    string? TransactionReference,
    DateTime? PaidAtUtc)
{
    public RecordFinancePaymentDto ToDto() => new(Amount, Method, TransactionReference, PaidAtUtc);
}

public sealed record UpdateFinancePaymentRequest(
    decimal Amount,
    string? Method,
    string? TransactionReference,
    DateTime? PaidAtUtc)
{
    public UpdateFinancePaymentDto ToDto() => new(Amount, Method, TransactionReference, PaidAtUtc);
}

public sealed record FinancialAdjustmentRequest(decimal Amount, string? Reason)
{
    public FinancialAdjustmentDto ToDto() => new(Amount, Reason);
}

public sealed record FinancePaymentStatusRequest(string? Status)
{
    public FinancePaymentStatusDto ToDto() => new(Status);
}
