using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Courses.RemoveCourseAssignment;

public sealed record RemoveCourseAssignmentCommand(Guid CourseId) : IRequest<bool>;
