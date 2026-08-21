using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Management;
using MediatR;

namespace InstituteManagement.Application.Features.Management.GetItems;

public sealed class GetManagementItemsHandler(IManagementService service) : IRequestHandler<GetManagementItemsQuery, IReadOnlyList<IManagementItemDto>>
{
    public Task<IReadOnlyList<IManagementItemDto>> Handle(GetManagementItemsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Resource, request.Search, request.DepartmentId, cancellationToken);
}
