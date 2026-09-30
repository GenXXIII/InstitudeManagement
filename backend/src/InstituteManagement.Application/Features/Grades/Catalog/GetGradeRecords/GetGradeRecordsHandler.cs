using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Grades.GetGradeRecords;

public sealed class GetGradeRecordsHandler(IGradeCatalogService service) : IRequestHandler<GetGradeRecordsQuery, PagedResult<GradeResponseDto>>
{
    public Task<PagedResult<GradeResponseDto>> Handle(GetGradeRecordsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, request.TeacherId, request.GroupBy, request.Status, request.Page, cancellationToken);
}
