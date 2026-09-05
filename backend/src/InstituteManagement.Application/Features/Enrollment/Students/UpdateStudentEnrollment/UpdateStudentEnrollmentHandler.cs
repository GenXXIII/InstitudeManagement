using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Students.UpdateStudentEnrollment;

public sealed class UpdateStudentEnrollmentHandler(IStudentEnrollmentService service)
    : IRequestHandler<UpdateStudentEnrollmentCommand, EnrollmentItemDto>
{
    public Task<EnrollmentItemDto> Handle(UpdateStudentEnrollmentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.StudentId, request.Values, cancellationToken);
}
