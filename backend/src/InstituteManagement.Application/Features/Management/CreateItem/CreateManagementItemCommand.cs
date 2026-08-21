using InstituteManagement.Application.DTOs.Management;
using MediatR;

namespace InstituteManagement.Application.Features.Management.CreateItem;

public sealed record CreateManagementItemCommand(string Resource, Dictionary<string, string> Values) : IRequest<IManagementItemDto>;
