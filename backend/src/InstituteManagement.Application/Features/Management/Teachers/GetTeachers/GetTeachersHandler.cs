using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.GetTeachers;

public sealed class GetTeachersHandler(ITeacherManagementService service) : IRequestHandler<GetTeachersQuery, IReadOnlyList<TeacherResponseDto>>
{
    public Task<IReadOnlyList<TeacherResponseDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
