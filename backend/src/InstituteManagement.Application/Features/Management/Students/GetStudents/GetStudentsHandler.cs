using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.GetStudents;

public sealed class GetStudentsHandler(IStudentManagementService service) : IRequestHandler<GetStudentsQuery, PagedResult<StudentResponseDto>>
{
    public Task<PagedResult<StudentResponseDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.ProfileId, request.Page, cancellationToken);
}
