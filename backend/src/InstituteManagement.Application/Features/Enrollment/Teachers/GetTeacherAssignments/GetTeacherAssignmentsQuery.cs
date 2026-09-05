using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.GetTeacherAssignments;

public sealed record GetTeacherAssignmentsQuery(string? Search, Guid? DepartmentId, int? Year)
    : IRequest<IReadOnlyList<EnrollmentItemDto>>;
