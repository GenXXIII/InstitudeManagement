namespace InstituteManagement.Application.Features.Finance;

public sealed record FinanceDeclarationDto(
    string? Title,
    string? PaymentPlan,
    decimal Amount,
    DateOnly DueOn,
    DateTime ExpiresAtUtc);

public sealed record RecordFinancePaymentDto(
    decimal Amount,
    string? Method,
    string? TransactionReference,
    DateTime? PaidAtUtc);

public sealed record UpdateFinancePaymentDto(
    decimal Amount,
    string? Method,
    string? TransactionReference,
    DateTime? PaidAtUtc);

public sealed record FinancialAdjustmentDto(decimal Amount, string? Reason);

public sealed record FinancePaymentStatusDto(string? Status);

public sealed record FinanceExpiryExtensionDto(int Days, string? Reason);

public sealed record BulkFinanceDeclarationResultDto(
    int DeclaredCount,
    DateTime AnnouncedAtUtc,
    DateOnly DueOn,
    DateTime ExpiresAtUtc);

public sealed record BankPaymentOptionDto(
    string Name,
    string AccountName,
    string AccountCode);

public sealed record FinanceOptionsDto(
    IReadOnlyList<string> PaymentMethods,
    bool AllowPartialPayments,
    bool AllowOverpayment,
    decimal MaximumAdjustmentAmount,
    bool RequirePaidForAdvancement,
    int PaymentDueDays,
    decimal SemesterPrice,
    decimal OtherFee,
    IReadOnlyList<BankPaymentOptionDto> PaymentProviders,
    bool BakongEnabled,
    bool BakongConfigured,
    string BakongEnvironment,
    string DynamicQrBank,
    string DynamicQrAccountName,
    string DynamicQrAccountCode);
