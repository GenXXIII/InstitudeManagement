using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Management.CreateItem;

public sealed record CreateManagementItemCommand(string Resource, Dictionary<string, string> Values) : IRequest<CatalogItemDto>;
