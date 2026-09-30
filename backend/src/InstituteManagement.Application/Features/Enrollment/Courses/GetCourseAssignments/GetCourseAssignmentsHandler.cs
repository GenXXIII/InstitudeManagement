using InstituteManagement.Application.Common.Pagination;
using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Courses.GetCourseAssignments;

public sealed class GetCourseAssignmentsHandler(ICourseAssignmentService service)
    : IRequestHandler<GetCourseAssignmentsQuery, PagedResult<EnrollmentItemDto>>
{
    public Task<PagedResult<EnrollmentItemDto>> Handle(GetCourseAssignmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, request.Page, cancellationToken);
}
