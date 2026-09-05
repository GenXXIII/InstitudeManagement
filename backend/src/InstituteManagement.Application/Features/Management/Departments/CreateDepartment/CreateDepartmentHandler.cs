using MediatR;

namespace InstituteManagement.Application.Features.Management.Departments.CreateDepartment;

public sealed class CreateDepartmentHandler(IDepartmentManagementService service) : IRequestHandler<CreateDepartmentCommand, DepartmentResponseDto>
{
    public Task<DepartmentResponseDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
