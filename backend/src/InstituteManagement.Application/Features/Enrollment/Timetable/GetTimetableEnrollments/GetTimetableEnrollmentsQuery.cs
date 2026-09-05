using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Timetable.GetTimetableEnrollments;

public sealed record GetTimetableEnrollmentsQuery(string? Search, Guid? DepartmentId, int? Year)
    : IRequest<IReadOnlyList<EnrollmentItemDto>>;
