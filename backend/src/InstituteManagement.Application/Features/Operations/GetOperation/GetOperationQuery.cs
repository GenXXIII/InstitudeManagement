using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Operations.GetOperation;

public sealed record GetOperationQuery(string Module, Guid? DepartmentId) : IRequest<OperationDto>;
