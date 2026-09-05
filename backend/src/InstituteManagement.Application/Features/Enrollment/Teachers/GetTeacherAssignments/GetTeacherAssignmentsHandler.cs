using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.GetTeacherAssignments;

public sealed class GetTeacherAssignmentsHandler(ITeacherAssignmentService service)
    : IRequestHandler<GetTeacherAssignmentsQuery, IReadOnlyList<EnrollmentItemDto>>
{
    public Task<IReadOnlyList<EnrollmentItemDto>> Handle(GetTeacherAssignmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, cancellationToken);
}
