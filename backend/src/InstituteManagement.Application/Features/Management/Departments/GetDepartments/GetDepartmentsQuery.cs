using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.GetDepartments;

public sealed record GetDepartmentsQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<DepartmentResponseDto>>;
