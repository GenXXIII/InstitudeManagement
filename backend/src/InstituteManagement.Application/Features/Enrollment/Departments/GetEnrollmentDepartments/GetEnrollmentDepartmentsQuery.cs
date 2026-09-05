using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Departments.GetEnrollmentDepartments;

public sealed record GetEnrollmentDepartmentsQuery(string? Search, Guid? DepartmentId, int? Year)
    : IRequest<IReadOnlyList<EnrollmentItemDto>>;
