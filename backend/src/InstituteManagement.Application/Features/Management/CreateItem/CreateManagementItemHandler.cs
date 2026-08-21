using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Management;
using MediatR;

namespace InstituteManagement.Application.Features.Management.CreateItem;

public sealed class CreateManagementItemHandler(IManagementService service) : IRequestHandler<CreateManagementItemCommand, IManagementItemDto>
{
    public Task<IManagementItemDto> Handle(CreateManagementItemCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Resource, request.Values, cancellationToken);
}
