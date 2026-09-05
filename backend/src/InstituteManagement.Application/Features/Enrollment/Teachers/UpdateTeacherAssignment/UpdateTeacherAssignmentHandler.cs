using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Teachers.UpdateTeacherAssignment;

public sealed class UpdateTeacherAssignmentHandler(ITeacherAssignmentService service)
    : IRequestHandler<UpdateTeacherAssignmentCommand, EnrollmentItemDto>
{
    public Task<EnrollmentItemDto> Handle(UpdateTeacherAssignmentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.TeacherId, request.Values, cancellationToken);
}
