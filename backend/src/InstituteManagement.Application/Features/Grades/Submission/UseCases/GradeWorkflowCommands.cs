using InstituteManagement.Application.Common.Exceptions;
using MediatR;

namespace InstituteManagement.Application.Features.Grades.UseCases;

public sealed record AttemptIndividualGradeSubmissionCommand : IRequest;

public sealed class AttemptIndividualGradeSubmissionHandler : IRequestHandler<AttemptIndividualGradeSubmissionCommand>
{
    public Task Handle(AttemptIndividualGradeSubmissionCommand request, CancellationToken cancellationToken) =>
        throw new BusinessConflictException(
            "Individual Student grade submission is not available.",
            "Submit the complete Teacher-assigned course roster through the course submission workflow.");
}

public sealed record RequestCourseGradeSubmissionCommand(
    Guid TeacherId,
    Guid CourseId,
    IReadOnlyList<GradeStudentScore> Students) : IRequest;

public sealed class RequestCourseGradeSubmissionHandler(IGradeService grades)
    : IRequestHandler<RequestCourseGradeSubmissionCommand>
{
    public Task Handle(RequestCourseGradeSubmissionCommand request, CancellationToken cancellationToken) =>
        grades.RequestCourseSubmissionAsync(request.TeacherId, request.CourseId, request.Students, cancellationToken);
}

public sealed record ReviewGradeSubmissionCommand(Guid GradeId, string Decision, string Note) : IRequest;

public sealed class ReviewGradeSubmissionHandler(IGradeService grades)
    : IRequestHandler<ReviewGradeSubmissionCommand>
{
    public Task Handle(ReviewGradeSubmissionCommand request, CancellationToken cancellationToken) =>
        grades.ReviewAsync(request.GradeId, request.Decision, request.Note, cancellationToken);
}

public sealed record SubmitAuthorizedGradeCommand(Guid GradeId, Guid TeacherId) : IRequest;

public sealed class SubmitAuthorizedGradeHandler(IGradeService grades)
    : IRequestHandler<SubmitAuthorizedGradeCommand>
{
    public Task Handle(SubmitAuthorizedGradeCommand request, CancellationToken cancellationToken) =>
        grades.SubmitAuthorizedAsync(request.GradeId, request.TeacherId, cancellationToken);
}

public sealed record SubmitAuthorizedCourseGradesCommand(
    Guid GradeId,
    Guid TeacherId,
    IReadOnlyList<GradeStudentScore> Students) : IRequest;

public sealed class SubmitAuthorizedCourseGradesHandler(IGradeService grades)
    : IRequestHandler<SubmitAuthorizedCourseGradesCommand>
{
    public Task Handle(SubmitAuthorizedCourseGradesCommand request, CancellationToken cancellationToken) =>
        grades.SubmitAuthorizedCourseAsync(request.GradeId, request.TeacherId, request.Students, cancellationToken);
}

public sealed record RequestCourseGradeResubmissionCommand(Guid GradeId, Guid TeacherId, string Note) : IRequest;

public sealed class RequestCourseGradeResubmissionHandler(IGradeService grades)
    : IRequestHandler<RequestCourseGradeResubmissionCommand>
{
    public Task Handle(RequestCourseGradeResubmissionCommand request, CancellationToken cancellationToken) =>
        grades.RequestCourseResubmissionAsync(request.GradeId, request.TeacherId, request.Note, cancellationToken);
}

public sealed record ConfirmReadyFinalGradesCommand(Guid? DepartmentId, int? Year) : IRequest<int>;

public sealed class ConfirmReadyFinalGradesHandler(IGradeService grades)
    : IRequestHandler<ConfirmReadyFinalGradesCommand, int>
{
    public Task<int> Handle(ConfirmReadyFinalGradesCommand request, CancellationToken cancellationToken) =>
        grades.ConfirmReadyFinalGradesAsync(request.DepartmentId, request.Year, cancellationToken);
}
