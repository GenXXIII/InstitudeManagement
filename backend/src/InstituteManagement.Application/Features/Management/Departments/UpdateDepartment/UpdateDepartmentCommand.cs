using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.UpdateDepartment;

public sealed record UpdateDepartmentCommand(Guid Id, Dictionary<string, string> Values) : IRequest<DepartmentResponseDto>;
