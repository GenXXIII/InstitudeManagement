using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Management.GetItems;

public sealed record GetManagementItemsQuery(string Resource, string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<CatalogItemDto>>;
