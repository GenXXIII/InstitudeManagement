namespace InstituteManagement.Application.Features.Finance;

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

public sealed record FinancePaymentStatusDto(string? Status);

public sealed record MockPaymentScanDto(string? QrPayload);

public sealed record MockPaymentQrDto(
    string QrPayload,
    string PublicId,
    DateTime GeneratedAtUtc,
    DateTime ExpiresAtUtc);
