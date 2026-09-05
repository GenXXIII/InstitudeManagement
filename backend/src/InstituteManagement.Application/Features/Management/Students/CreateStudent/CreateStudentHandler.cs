using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.CreateStudent;

public sealed class CreateStudentHandler(IStudentManagementService service) : IRequestHandler<CreateStudentCommand, StudentResponseDto>
{
    public Task<StudentResponseDto> Handle(CreateStudentCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
