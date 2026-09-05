using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.DeleteStudent;

public sealed class DeleteStudentHandler(IStudentManagementService service) : IRequestHandler<DeleteStudentCommand, bool>
{
    public Task<bool> Handle(DeleteStudentCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
