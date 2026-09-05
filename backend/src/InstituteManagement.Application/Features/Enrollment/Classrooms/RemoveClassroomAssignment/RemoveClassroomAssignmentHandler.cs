using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms.RemoveClassroomAssignment;

public sealed class RemoveClassroomAssignmentHandler(IClassroomAssignmentService service)
    : IRequestHandler<RemoveClassroomAssignmentCommand, bool>
{
    public Task<bool> Handle(RemoveClassroomAssignmentCommand request, CancellationToken cancellationToken) =>
        service.RemoveAsync(request.ClassroomId, cancellationToken);
}
