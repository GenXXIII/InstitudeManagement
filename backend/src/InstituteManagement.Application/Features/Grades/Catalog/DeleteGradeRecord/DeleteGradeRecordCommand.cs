using MediatR;

namespace InstituteManagement.Application.Features.Grades.DeleteGradeRecord;

public sealed record DeleteGradeRecordCommand(Guid Id) : IRequest<bool>;
