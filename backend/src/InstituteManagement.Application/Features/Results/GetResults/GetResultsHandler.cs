using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Results;
using MediatR;

namespace InstituteManagement.Application.Features.Results.GetResults;

public sealed class GetResultsHandler(IResultQueryService service) : IRequestHandler<GetResultsQuery, PagedResult<SemesterResultDto>>
{
    public Task<PagedResult<SemesterResultDto>> Handle(GetResultsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.DepartmentId, request.Year, request.Semester, request.AcademicYear, request.History, request.StudentId, request.PublishedOnly, request.Search, request.Outcome, request.Page, cancellationToken);
}
