using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Students.RemoveStudentEnrollment;

public sealed record RemoveStudentEnrollmentCommand(Guid StudentId) : IRequest<bool>;
