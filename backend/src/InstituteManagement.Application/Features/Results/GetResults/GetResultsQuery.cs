using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Results;
using MediatR;

namespace InstituteManagement.Application.Features.Results.GetResults;

public sealed record GetResultsQuery(Guid? DepartmentId, int? Year, string? Semester, string? AcademicYear, bool History, Guid? StudentId, bool PublishedOnly, string? Search, string? Outcome, PageRequest Page) : IRequest<PagedResult<SemesterResultDto>>;
