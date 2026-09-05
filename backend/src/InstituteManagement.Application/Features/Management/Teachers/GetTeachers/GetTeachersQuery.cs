using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.GetTeachers;

public sealed record GetTeachersQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<TeacherResponseDto>>;
