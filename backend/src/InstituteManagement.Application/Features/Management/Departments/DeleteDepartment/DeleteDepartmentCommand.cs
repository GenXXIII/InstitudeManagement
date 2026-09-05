using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.DeleteDepartment;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest<bool>;
