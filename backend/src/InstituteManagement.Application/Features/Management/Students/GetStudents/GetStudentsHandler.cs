using MediatR;

namespace InstituteManagement.Application.Features.Management.Students.GetStudents;

public sealed class GetStudentsHandler(IStudentManagementService service) : IRequestHandler<GetStudentsQuery, IReadOnlyList<StudentResponseDto>>
{
    public Task<IReadOnlyList<StudentResponseDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
