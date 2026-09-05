using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Courses.GetCourseAssignments;

public sealed class GetCourseAssignmentsHandler(ICourseAssignmentService service)
    : IRequestHandler<GetCourseAssignmentsQuery, IReadOnlyList<EnrollmentItemDto>>
{
    public Task<IReadOnlyList<EnrollmentItemDto>> Handle(GetCourseAssignmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, cancellationToken);
}
