using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.GetClassrooms;

public sealed class GetClassroomsHandler(IClassroomManagementService service) : IRequestHandler<GetClassroomsQuery, IReadOnlyList<ClassroomResponseDto>>
{
    public Task<IReadOnlyList<ClassroomResponseDto>> Handle(GetClassroomsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
