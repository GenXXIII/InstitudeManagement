using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.GetStudents;

public sealed record GetStudentsQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<StudentResponseDto>>;
