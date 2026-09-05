using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.RemoveTeacherAssignment;

public sealed record RemoveTeacherAssignmentCommand(Guid TeacherId) : IRequest<bool>;
