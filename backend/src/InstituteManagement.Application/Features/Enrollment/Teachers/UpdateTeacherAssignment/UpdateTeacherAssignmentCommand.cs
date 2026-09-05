using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.UpdateTeacherAssignment;

public sealed record UpdateTeacherAssignmentCommand(Guid TeacherId, Dictionary<string, string> Values)
    : IRequest<EnrollmentItemDto>;
