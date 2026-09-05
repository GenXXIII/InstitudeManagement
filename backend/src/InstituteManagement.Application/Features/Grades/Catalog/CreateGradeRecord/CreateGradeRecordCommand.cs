using MediatR;

namespace InstituteManagement.Application.Features.Grades.CreateGradeRecord;

public sealed record CreateGradeRecordCommand(Dictionary<string, string> Values) : IRequest<GradeResponseDto>;
