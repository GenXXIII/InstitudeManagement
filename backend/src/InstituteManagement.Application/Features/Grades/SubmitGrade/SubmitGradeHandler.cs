using InstituteManagement.Application.Abstractions;
using MediatR;

namespace InstituteManagement.Application.Features.Grades.SubmitGrade;

public sealed class SubmitGradeHandler(IGradeService service, ILiveUpdatePublisher publisher) : IRequestHandler<SubmitGradeCommand>
{
    public async Task Handle(SubmitGradeCommand request, CancellationToken cancellationToken)
    {
        await service.SubmitAsync(request.StudentId, request.CourseId, request.Score, cancellationToken);
        await publisher.PublishAsync("GRADE_SUBMITTED", new { request.StudentId, request.CourseId, request.Score }, cancellationToken);
    }
}
