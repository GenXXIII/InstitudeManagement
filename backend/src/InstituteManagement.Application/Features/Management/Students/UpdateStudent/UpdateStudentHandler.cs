using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.UpdateStudent;

public sealed class UpdateStudentHandler(IStudentManagementService service) : IRequestHandler<UpdateStudentCommand, StudentResponseDto>
{
    public Task<StudentResponseDto> Handle(UpdateStudentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
