using MediatR;

namespace InstituteManagement.Application.Features.Management.DeleteItem;

public sealed record DeleteManagementItemCommand(string Resource, Guid Id) : IRequest<bool>;
