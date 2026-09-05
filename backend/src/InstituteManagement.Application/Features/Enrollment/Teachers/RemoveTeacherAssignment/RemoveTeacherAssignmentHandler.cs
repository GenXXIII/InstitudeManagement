using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.RemoveTeacherAssignment;

public sealed class RemoveTeacherAssignmentHandler(ITeacherAssignmentService service)
    : IRequestHandler<RemoveTeacherAssignmentCommand, bool>
{
    public Task<bool> Handle(RemoveTeacherAssignmentCommand request, CancellationToken cancellationToken) =>
        service.RemoveAsync(request.TeacherId, cancellationToken);
}
