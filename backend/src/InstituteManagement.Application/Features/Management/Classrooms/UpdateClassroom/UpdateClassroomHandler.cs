using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.UpdateClassroom;

public sealed class UpdateClassroomHandler(IClassroomManagementService service) : IRequestHandler<UpdateClassroomCommand, ClassroomResponseDto>
{
    public Task<ClassroomResponseDto> Handle(UpdateClassroomCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
