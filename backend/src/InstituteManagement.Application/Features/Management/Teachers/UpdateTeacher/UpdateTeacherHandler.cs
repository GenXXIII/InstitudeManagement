using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.UpdateTeacher;

public sealed class UpdateTeacherHandler(ITeacherManagementService service) : IRequestHandler<UpdateTeacherCommand, TeacherResponseDto>
{
    public Task<TeacherResponseDto> Handle(UpdateTeacherCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
