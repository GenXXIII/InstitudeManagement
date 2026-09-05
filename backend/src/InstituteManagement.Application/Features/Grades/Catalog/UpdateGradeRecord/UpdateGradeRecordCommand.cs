using MediatR;

namespace InstituteManagement.Application.Features.Grades.UpdateGradeRecord;

public sealed record UpdateGradeRecordCommand(Guid Id, Dictionary<string, string> Values) : IRequest<GradeResponseDto>;
