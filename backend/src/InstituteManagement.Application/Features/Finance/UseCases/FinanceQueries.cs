using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Finance.UseCases;

public sealed record GetFinanceAccountsQuery(
    string? Search,
    string? AcademicYear,
    string? Semester,
    string? Status,
    Guid? DepartmentId,
    int? Year,
    PageRequest Page) : IRequest<PagedResult<StudentPaymentDto>>;

public sealed class GetFinanceAccountsHandler(IFinanceService finance)
    : IRequestHandler<GetFinanceAccountsQuery, PagedResult<StudentPaymentDto>>
{
    public Task<PagedResult<StudentPaymentDto>> Handle(GetFinanceAccountsQuery request, CancellationToken cancellationToken) =>
        finance.GetAsync(
            request.Search,
            request.AcademicYear,
            request.Semester,
            request.Status,
            request.DepartmentId,
            request.Year,
            request.Page,
            cancellationToken);
}

public sealed record GetStudentFinanceAccountsQuery(Guid StudentId) : IRequest<IReadOnlyList<StudentPaymentDto>>;

public sealed class GetStudentFinanceAccountsHandler(IFinanceService finance)
    : IRequestHandler<GetStudentFinanceAccountsQuery, IReadOnlyList<StudentPaymentDto>>
{
    public Task<IReadOnlyList<StudentPaymentDto>> Handle(GetStudentFinanceAccountsQuery request, CancellationToken cancellationToken) =>
        finance.GetStudentAsync(request.StudentId, cancellationToken);
}

public sealed record GetFinanceOptionsQuery : IRequest<FinanceOptionsDto>;

public sealed class GetFinanceOptionsHandler(IFinanceService finance)
    : IRequestHandler<GetFinanceOptionsQuery, FinanceOptionsDto>
{
    public Task<FinanceOptionsDto> Handle(GetFinanceOptionsQuery request, CancellationToken cancellationToken) =>
        finance.GetOptionsAsync(cancellationToken);
}

public sealed record GetFinanceClosureReadinessQuery(Guid? DepartmentId, int? Year)
    : IRequest<FinanceClosureReadinessDto>;

public sealed class GetFinanceClosureReadinessHandler(IFinanceService finance)
    : IRequestHandler<GetFinanceClosureReadinessQuery, FinanceClosureReadinessDto>
{
    public Task<FinanceClosureReadinessDto> Handle(GetFinanceClosureReadinessQuery request, CancellationToken cancellationToken) =>
        finance.GetClosureReadinessAsync(request.DepartmentId, request.Year, cancellationToken);
}
