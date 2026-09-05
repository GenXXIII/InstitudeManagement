using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.CreateTeacher;

public sealed class CreateTeacherHandler(ITeacherManagementService service) : IRequestHandler<CreateTeacherCommand, TeacherResponseDto>
{
    public Task<TeacherResponseDto> Handle(CreateTeacherCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
