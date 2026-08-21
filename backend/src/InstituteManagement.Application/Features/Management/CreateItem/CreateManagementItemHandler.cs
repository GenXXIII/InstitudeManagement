using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Management.CreateItem;

public sealed class CreateManagementItemHandler(IManagementService service) : IRequestHandler<CreateManagementItemCommand, CatalogItemDto>
{
    public Task<CatalogItemDto> Handle(CreateManagementItemCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Resource, request.Values, cancellationToken);
}
