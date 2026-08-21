using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Management;
using MediatR;

namespace InstituteManagement.Application.Features.Management.UpdateItem;

public sealed class UpdateManagementItemHandler(IManagementService service) : IRequestHandler<UpdateManagementItemCommand, IManagementItemDto>
{
    public Task<IManagementItemDto> Handle(UpdateManagementItemCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Resource, request.Id, request.Values, cancellationToken);
}
