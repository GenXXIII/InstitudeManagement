using MediatR;

namespace InstituteManagement.Application.Features.Grades.GetGradeRecords;

public sealed class GetGradeRecordsHandler(IGradeCatalogService service) : IRequestHandler<GetGradeRecordsQuery, IReadOnlyList<GradeResponseDto>>
{
    public Task<IReadOnlyList<GradeResponseDto>> Handle(GetGradeRecordsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
