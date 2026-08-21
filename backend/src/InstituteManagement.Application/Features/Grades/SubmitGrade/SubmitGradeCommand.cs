using MediatR;

namespace InstituteManagement.Application.Features.Grades.SubmitGrade;

public sealed record SubmitGradeCommand(Guid StudentId, Guid CourseId, decimal Score) : IRequest;
