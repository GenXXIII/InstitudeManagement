using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.GetClassrooms;

public sealed record GetClassroomsQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<ClassroomResponseDto>>;
