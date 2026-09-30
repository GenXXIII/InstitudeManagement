using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.GetTeachers;

public sealed class GetTeachersHandler(ITeacherManagementService service) : IRequestHandler<GetTeachersQuery, PagedResult<TeacherResponseDto>>
{
    public Task<PagedResult<TeacherResponseDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.ProfileId, request.Page, cancellationToken);
}
