using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.GetDepartments;

public sealed class GetDepartmentsHandler(IDepartmentManagementService service) : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentResponseDto>>
{
    public Task<IReadOnlyList<DepartmentResponseDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.DepartmentId, cancellationToken);
}
