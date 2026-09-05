using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.DeleteDepartment;

public sealed class DeleteDepartmentHandler(IDepartmentManagementService service) : IRequestHandler<DeleteDepartmentCommand, bool>
{
    public Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
