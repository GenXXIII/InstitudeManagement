using MediatR;

namespace InstituteManagement.Application.Features.Finance.UseCases;

public sealed record DeclareFinanceAccountCommand(Guid FinancialAccountId, FinanceDeclarationDto Declaration)
    : IRequest<StudentPaymentDto>;

public sealed class DeclareFinanceAccountHandler(IFinanceService finance)
    : IRequestHandler<DeclareFinanceAccountCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(DeclareFinanceAccountCommand request, CancellationToken cancellationToken) =>
        finance.DeclareAsync(request.FinancialAccountId, request.Declaration, cancellationToken);
}

public sealed record DeclareAllFinanceAccountsCommand(Guid? DepartmentId, int? Year)
    : IRequest<BulkFinanceDeclarationResultDto>;

public sealed class DeclareAllFinanceAccountsHandler(IFinanceService finance)
    : IRequestHandler<DeclareAllFinanceAccountsCommand, BulkFinanceDeclarationResultDto>
{
    public Task<BulkFinanceDeclarationResultDto> Handle(DeclareAllFinanceAccountsCommand request, CancellationToken cancellationToken) =>
        finance.DeclareAllAsync(request.DepartmentId, request.Year, cancellationToken);
}

public sealed record CloseAllFinancePaymentsCommand(Guid? DepartmentId, int? Year)
    : IRequest<BulkFinanceClosureResultDto>;

public sealed class CloseAllFinancePaymentsHandler(IFinanceService finance)
    : IRequestHandler<CloseAllFinancePaymentsCommand, BulkFinanceClosureResultDto>
{
    public Task<BulkFinanceClosureResultDto> Handle(CloseAllFinancePaymentsCommand request, CancellationToken cancellationToken) =>
        finance.CloseAllPaymentsAsync(request.DepartmentId, request.Year, cancellationToken);
}

public sealed record ExtendFinanceExpiryCommand(Guid FinancialAccountId, FinanceExpiryExtensionDto Extension)
    : IRequest<StudentPaymentDto>;

public sealed class ExtendFinanceExpiryHandler(IFinanceService finance)
    : IRequestHandler<ExtendFinanceExpiryCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(ExtendFinanceExpiryCommand request, CancellationToken cancellationToken) =>
        finance.ExtendExpiryAsync(request.FinancialAccountId, request.Extension, cancellationToken);
}

public sealed record RegenerateFinanceQrCommand(Guid FinancialAccountId) : IRequest<StudentPaymentDto>;

public sealed class RegenerateFinanceQrHandler(IFinanceService finance)
    : IRequestHandler<RegenerateFinanceQrCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(RegenerateFinanceQrCommand request, CancellationToken cancellationToken) =>
        finance.RegenerateQrAsync(request.FinancialAccountId, cancellationToken);
}

public sealed record GenerateMockFinanceQrCommand(Guid FinancialAccountId) : IRequest<MockPaymentQrDto>;

public sealed class GenerateMockFinanceQrHandler(IFinanceService finance)
    : IRequestHandler<GenerateMockFinanceQrCommand, MockPaymentQrDto>
{
    public Task<MockPaymentQrDto> Handle(GenerateMockFinanceQrCommand request, CancellationToken cancellationToken) =>
        finance.GenerateMockPaymentQrAsync(request.FinancialAccountId, cancellationToken);
}

public sealed record CancelFinanceAccountCommand(Guid FinancialAccountId) : IRequest<StudentPaymentDto>;

public sealed class CancelFinanceAccountHandler(IFinanceService finance)
    : IRequestHandler<CancelFinanceAccountCommand, StudentPaymentDto>
{
    public Task<StudentPaymentDto> Handle(CancelFinanceAccountCommand request, CancellationToken cancellationToken) =>
        finance.CancelAsync(request.FinancialAccountId, cancellationToken);
}
