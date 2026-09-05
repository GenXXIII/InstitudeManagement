using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Courses.UpdateCourseAssignment;

public sealed class UpdateCourseAssignmentHandler(ICourseAssignmentService service)
    : IRequestHandler<UpdateCourseAssignmentCommand, EnrollmentItemDto>
{
    public Task<EnrollmentItemDto> Handle(UpdateCourseAssignmentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.CourseId, request.Values, cancellationToken);
}
