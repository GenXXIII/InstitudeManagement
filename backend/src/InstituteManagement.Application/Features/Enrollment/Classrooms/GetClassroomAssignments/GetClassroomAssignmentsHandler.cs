using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms.GetClassroomAssignments;

public sealed class GetClassroomAssignmentsHandler(IClassroomAssignmentService service)
    : IRequestHandler<GetClassroomAssignmentsQuery, IReadOnlyList<EnrollmentItemDto>>
{
    public Task<IReadOnlyList<EnrollmentItemDto>> Handle(GetClassroomAssignmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, cancellationToken);
}
