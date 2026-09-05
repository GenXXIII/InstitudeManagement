using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Classrooms.UpdateClassroomAssignment;

public sealed class UpdateClassroomAssignmentHandler(IClassroomAssignmentService service)
    : IRequestHandler<UpdateClassroomAssignmentCommand, EnrollmentItemDto>
{
    public Task<EnrollmentItemDto> Handle(UpdateClassroomAssignmentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.ClassroomId, request.Values, cancellationToken);
}
