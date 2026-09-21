using MediatR;

namespace InstituteManagement.Application.Features.Grades.SubmitGrade;

public sealed record SubmitGradeCommand(Guid StudentId, Guid CourseId, Guid TeacherId, decimal AssignmentScore, decimal MidtermScore, decimal FinalExamScore) : IRequest;
