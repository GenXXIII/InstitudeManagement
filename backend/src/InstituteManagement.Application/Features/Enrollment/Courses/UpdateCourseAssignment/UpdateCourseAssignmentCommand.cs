using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Courses.UpdateCourseAssignment;

public sealed record UpdateCourseAssignmentCommand(Guid CourseId, Dictionary<string, string> Values)
    : IRequest<EnrollmentItemDto>;
