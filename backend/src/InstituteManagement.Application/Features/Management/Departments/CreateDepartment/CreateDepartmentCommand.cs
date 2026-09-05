using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.CreateDepartment;

public sealed record CreateDepartmentCommand(Dictionary<string, string> Values) : IRequest<DepartmentResponseDto>;
