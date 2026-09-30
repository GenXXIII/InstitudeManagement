using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.GetTeachers;

public sealed record GetTeachersQuery(string? Search, Guid? DepartmentId, Guid? ProfileId, PageRequest Page) : IRequest<PagedResult<TeacherResponseDto>>;
