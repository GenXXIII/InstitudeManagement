using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Grades.GetGradeRecords;

public sealed record GetGradeRecordsQuery(string? Search, Guid? DepartmentId, int? Year, Guid? TeacherId, string? GroupBy, string? Status, PageRequest Page) : IRequest<PagedResult<GradeResponseDto>>;
