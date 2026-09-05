using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Students.GetStudentEnrollments;

public sealed record GetStudentEnrollmentsQuery(string? Search, Guid? DepartmentId, int? Year)
    : IRequest<IReadOnlyList<EnrollmentItemDto>>;
