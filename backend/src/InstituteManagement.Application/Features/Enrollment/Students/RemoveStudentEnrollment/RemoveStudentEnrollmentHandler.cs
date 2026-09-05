using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Students.RemoveStudentEnrollment;

public sealed class RemoveStudentEnrollmentHandler(IStudentEnrollmentService service)
    : IRequestHandler<RemoveStudentEnrollmentCommand, bool>
{
    public Task<bool> Handle(RemoveStudentEnrollmentCommand request, CancellationToken cancellationToken) =>
        service.RemoveAsync(request.StudentId, cancellationToken);
}
