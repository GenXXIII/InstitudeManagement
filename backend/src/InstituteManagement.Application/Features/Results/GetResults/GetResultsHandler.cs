using InstituteManagement.Application.Features.Results;
using MediatR;

namespace InstituteManagement.Application.Features.Results.GetResults;

public sealed class GetResultsHandler(IResultQueryService service) : IRequestHandler<GetResultsQuery, IReadOnlyList<SemesterResultDto>>
{
    public Task<IReadOnlyList<SemesterResultDto>> Handle(GetResultsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.DepartmentId, request.Year, request.Semester, request.AcademicYear, request.History, cancellationToken);
}
