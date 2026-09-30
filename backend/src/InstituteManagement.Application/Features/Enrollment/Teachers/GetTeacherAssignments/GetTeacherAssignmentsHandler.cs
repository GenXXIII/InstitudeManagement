using InstituteManagement.Application.Common.Pagination;
using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.GetTeacherAssignments;

public sealed class GetTeacherAssignmentsHandler(ITeacherAssignmentService service)
    : IRequestHandler<GetTeacherAssignmentsQuery, PagedResult<EnrollmentItemDto>>
{
    public Task<PagedResult<EnrollmentItemDto>> Handle(GetTeacherAssignmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, request.Page, cancellationToken);
}
