using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Management.GetItems;

public sealed class GetManagementItemsHandler(IManagementService service) : IRequestHandler<GetManagementItemsQuery, IReadOnlyList<CatalogItemDto>>
{
    public Task<IReadOnlyList<CatalogItemDto>> Handle(GetManagementItemsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Resource, request.Search, request.DepartmentId, cancellationToken);
}
