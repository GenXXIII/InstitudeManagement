namespace InstituteManagement.Application.Features.Finance;

public interface IFinanceService
{
    Task<IReadOnlyList<StudentPaymentDto>> GetAsync(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentPaymentDto>> GetStudentAsync(Guid studentId, CancellationToken cancellationToken);

    Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken cancellationToken);

    Task<StudentPaymentDto> DeclareAsync(
        Guid financialAccountId,
        FinanceDeclarationDto request,
        CancellationToken cancellationToken);

    Task<BulkFinanceDeclarationResultDto> DeclareAllAsync(CancellationToken cancellationToken);

    Task<StudentPaymentDto> ExtendExpiryAsync(
        Guid financialAccountId,
        FinanceExpiryExtensionDto request,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> RegenerateQrAsync(Guid financialAccountId, CancellationToken cancellationToken);

    Task<StudentPaymentDto> GenerateStudentQrAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> VerifyStudentPaymentAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> MarkReminderReadAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> RecordPaymentAsync(
        Guid financialAccountId,
        RecordFinancePaymentDto request,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> UpdatePaymentAsync(
        Guid financialAccountId,
        Guid paymentId,
        UpdateFinancePaymentDto request,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> SetPaymentStatusAsync(
        Guid financialAccountId,
        Guid paymentId,
        FinancePaymentStatusDto request,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> AdjustAsync(
        Guid financialAccountId,
        FinancialAdjustmentDto request,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> CancelAsync(Guid financialAccountId, CancellationToken cancellationToken);
}
