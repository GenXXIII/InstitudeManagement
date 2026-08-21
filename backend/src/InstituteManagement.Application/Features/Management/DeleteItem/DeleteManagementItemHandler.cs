using InstituteManagement.Application.Abstractions;
using MediatR;

namespace InstituteManagement.Application.Features.Management.DeleteItem;

public sealed class DeleteManagementItemHandler(IManagementService service) : IRequestHandler<DeleteManagementItemCommand, bool>
{
    public Task<bool> Handle(DeleteManagementItemCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Resource, request.Id, cancellationToken);
}
