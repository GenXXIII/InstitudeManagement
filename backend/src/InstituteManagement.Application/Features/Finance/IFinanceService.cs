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

    Task<StudentPaymentDto> ConfirmAsync(
        Guid studentId,
        Guid paymentId,
        StudentPaymentConfirmationDto request,
        CancellationToken cancellationToken);

    Task<StudentPaymentDto> MarkReminderReadAsync(
        Guid studentId,
        Guid paymentId,
        CancellationToken cancellationToken);
}
