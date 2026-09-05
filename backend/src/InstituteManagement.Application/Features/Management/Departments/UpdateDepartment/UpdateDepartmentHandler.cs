using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.UpdateDepartment;

public sealed class UpdateDepartmentHandler(IDepartmentManagementService service) : IRequestHandler<UpdateDepartmentCommand, DepartmentResponseDto>
{
    public Task<DepartmentResponseDto> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
