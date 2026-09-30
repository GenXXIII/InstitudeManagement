using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.GetStudents;

public sealed record GetStudentsQuery(string? Search, Guid? DepartmentId, Guid? ProfileId, PageRequest Page) : IRequest<PagedResult<StudentResponseDto>>;
