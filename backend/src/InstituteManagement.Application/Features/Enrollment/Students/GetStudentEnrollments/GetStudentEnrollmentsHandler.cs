using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Students.GetStudentEnrollments;

public sealed class GetStudentEnrollmentsHandler(IStudentEnrollmentService service)
    : IRequestHandler<GetStudentEnrollmentsQuery, IReadOnlyList<EnrollmentItemDto>>
{
    public Task<IReadOnlyList<EnrollmentItemDto>> Handle(GetStudentEnrollmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, cancellationToken);
}
