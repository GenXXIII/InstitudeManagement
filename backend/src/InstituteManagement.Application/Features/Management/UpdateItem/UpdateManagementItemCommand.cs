using InstituteManagement.Application.DTOs.Management;
using MediatR;

namespace InstituteManagement.Application.Features.Management.UpdateItem;

public sealed record UpdateManagementItemCommand(string Resource, Guid Id, Dictionary<string, string> Values) : IRequest<IManagementItemDto>;
