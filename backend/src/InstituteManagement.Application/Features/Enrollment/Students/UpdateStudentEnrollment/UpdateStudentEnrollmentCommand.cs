using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Students.UpdateStudentEnrollment;

public sealed record UpdateStudentEnrollmentCommand(Guid StudentId, Dictionary<string, string> Values)
    : IRequest<EnrollmentItemDto>;
