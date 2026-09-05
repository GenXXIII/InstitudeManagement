using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Courses.RemoveCourseAssignment;

public sealed class RemoveCourseAssignmentHandler(ICourseAssignmentService service)
    : IRequestHandler<RemoveCourseAssignmentCommand, bool>
{
    public Task<bool> Handle(RemoveCourseAssignmentCommand request, CancellationToken cancellationToken) =>
        service.RemoveAsync(request.CourseId, cancellationToken);
}
