using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms.RemoveClassroomAssignment;

public sealed record RemoveClassroomAssignmentCommand(Guid ClassroomId) : IRequest<bool>;
