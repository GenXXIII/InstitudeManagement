using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.DeleteClassroom;

public sealed class DeleteClassroomHandler(IClassroomManagementService service) : IRequestHandler<DeleteClassroomCommand, bool>
{
    public Task<bool> Handle(DeleteClassroomCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
