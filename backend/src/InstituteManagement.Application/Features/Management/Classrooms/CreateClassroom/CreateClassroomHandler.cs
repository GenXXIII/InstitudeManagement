using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.CreateClassroom;

public sealed class CreateClassroomHandler(IClassroomManagementService service) : IRequestHandler<CreateClassroomCommand, ClassroomResponseDto>
{
    public Task<ClassroomResponseDto> Handle(CreateClassroomCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
