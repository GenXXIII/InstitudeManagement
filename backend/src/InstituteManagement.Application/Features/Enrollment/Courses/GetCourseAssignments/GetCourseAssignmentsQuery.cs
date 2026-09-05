using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Courses.GetCourseAssignments;

public sealed record GetCourseAssignmentsQuery(string? Search, Guid? DepartmentId, int? Year)
    : IRequest<IReadOnlyList<EnrollmentItemDto>>;
