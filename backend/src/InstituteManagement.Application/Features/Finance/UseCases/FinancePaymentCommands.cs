using MediatR;

namespace InstituteManagement.Application.Features.Finance.UseCases;

public sealed record RecordFinancePaymentCommand(Guid FinancialAccountId, RecordFinancePaymentDto Payment)
    : IRequest<StudentPaymentDto>;

public sealed class RecordFinancePaymentHandler(IFinanceService finance)
    : IRequestHandler<RecordFinancePaymentCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(RecordFinancePaymentCommand request, CancellationToken cancellationToken) =>
        finance.RecordPaymentAsync(request.FinancialAccountId, request.Payment, cancellationToken);
}

public sealed record UpdateFinancePaymentCommand(Guid FinancialAccountId, Guid PaymentId, UpdateFinancePaymentDto Payment)
    : IRequest<StudentPaymentDto>;

public sealed class UpdateFinancePaymentHandler(IFinanceService finance)
    : IRequestHandler<UpdateFinancePaymentCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(UpdateFinancePaymentCommand request, CancellationToken cancellationToken) =>
        finance.UpdatePaymentAsync(request.FinancialAccountId, request.PaymentId, request.Payment, cancellationToken);
}

public sealed record SetFinancePaymentStatusCommand(Guid FinancialAccountId, Guid PaymentId, FinancePaymentStatusDto Status)
    : IRequest<StudentPaymentDto>;

public sealed class SetFinancePaymentStatusHandler(IFinanceService finance)
    : IRequestHandler<SetFinancePaymentStatusCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(SetFinancePaymentStatusCommand request, CancellationToken cancellationToken) =>
        finance.SetPaymentStatusAsync(request.FinancialAccountId, request.PaymentId, request.Status, cancellationToken);
}

public sealed record AdjustFinanceAccountCommand(Guid FinancialAccountId, FinancialAdjustmentDto Adjustment)
    : IRequest<StudentPaymentDto>;

public sealed class AdjustFinanceAccountHandler(IFinanceService finance)
    : IRequestHandler<AdjustFinanceAccountCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(AdjustFinanceAccountCommand request, CancellationToken cancellationToken) =>
        finance.AdjustAsync(request.FinancialAccountId, request.Adjustment, cancellationToken);
}
