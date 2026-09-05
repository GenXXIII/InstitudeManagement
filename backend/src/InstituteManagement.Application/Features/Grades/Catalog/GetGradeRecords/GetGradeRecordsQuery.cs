using MediatR;

namespace InstituteManagement.Application.Features.Grades.GetGradeRecords;

public sealed record GetGradeRecordsQuery(string? Search, Guid? DepartmentId) : IRequest<IReadOnlyList<GradeResponseDto>>;
