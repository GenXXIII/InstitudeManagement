using MediatR;

namespace InstituteManagement.Application.Features.Finance.UseCases;

public sealed record GenerateStudentPaymentQrCommand(Guid StudentId, Guid PaymentId) : IRequest<StudentPaymentDto>;

public sealed class GenerateStudentPaymentQrHandler(IFinanceService finance)
    : IRequestHandler<GenerateStudentPaymentQrCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(GenerateStudentPaymentQrCommand request, CancellationToken cancellationToken) =>
        finance.GenerateStudentQrAsync(request.StudentId, request.PaymentId, cancellationToken);
}

public sealed record VerifyStudentPaymentCommand(Guid StudentId, Guid PaymentId) : IRequest<StudentPaymentDto>;

public sealed class VerifyStudentPaymentHandler(IFinanceService finance)
    : IRequestHandler<VerifyStudentPaymentCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(VerifyStudentPaymentCommand request, CancellationToken cancellationToken) =>
        finance.VerifyStudentPaymentAsync(request.StudentId, request.PaymentId, cancellationToken);
}

public sealed record ScanMockPaymentQrCommand(Guid StudentId, Guid PaymentId, MockPaymentScanDto Scan)
    : IRequest<StudentPaymentDto>;

public sealed class ScanMockPaymentQrHandler(IFinanceService finance)
    : IRequestHandler<ScanMockPaymentQrCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(ScanMockPaymentQrCommand request, CancellationToken cancellationToken) =>
        finance.ScanMockPaymentQrAsync(request.StudentId, request.PaymentId, request.Scan, cancellationToken);
}

public sealed record MarkFinanceReminderReadCommand(Guid StudentId, Guid PaymentId) : IRequest<StudentPaymentDto>;

public sealed class MarkFinanceReminderReadHandler(IFinanceService finance)
    : IRequestHandler<MarkFinanceReminderReadCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(MarkFinanceReminderReadCommand request, CancellationToken cancellationToken) =>
        finance.MarkReminderReadAsync(request.StudentId, request.PaymentId, cancellationToken);
}
