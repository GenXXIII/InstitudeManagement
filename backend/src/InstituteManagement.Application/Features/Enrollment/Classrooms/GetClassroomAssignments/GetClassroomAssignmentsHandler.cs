using InstituteManagement.Application.Common.Pagination;
using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms.GetClassroomAssignments;

public sealed class GetClassroomAssignmentsHandler(IClassroomAssignmentService service)
    : IRequestHandler<GetClassroomAssignmentsQuery, PagedResult<EnrollmentItemDto>>
{
    public Task<PagedResult<EnrollmentItemDto>> Handle(GetClassroomAssignmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, request.Page, cancellationToken);
}
