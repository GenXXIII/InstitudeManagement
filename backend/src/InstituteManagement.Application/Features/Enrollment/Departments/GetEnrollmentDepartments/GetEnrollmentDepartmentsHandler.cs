using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Departments.GetEnrollmentDepartments;

public sealed class GetEnrollmentDepartmentsHandler(IDepartmentEnrollmentService service)
    : IRequestHandler<GetEnrollmentDepartmentsQuery, IReadOnlyList<EnrollmentItemDto>>
{
    public Task<IReadOnlyList<EnrollmentItemDto>> Handle(GetEnrollmentDepartmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, request.Year, cancellationToken);
}
