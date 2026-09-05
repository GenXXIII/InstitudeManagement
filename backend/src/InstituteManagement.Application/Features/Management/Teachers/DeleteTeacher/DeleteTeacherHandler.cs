using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.DeleteTeacher;

public sealed class DeleteTeacherHandler(ITeacherManagementService service) : IRequestHandler<DeleteTeacherCommand, bool>
{
    public Task<bool> Handle(DeleteTeacherCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
