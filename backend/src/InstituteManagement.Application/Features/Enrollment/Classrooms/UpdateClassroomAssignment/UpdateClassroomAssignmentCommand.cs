using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms.UpdateClassroomAssignment;

public sealed record UpdateClassroomAssignmentCommand(Guid ClassroomId, Dictionary<string, string> Values)
    : IRequest<EnrollmentItemDto>;
