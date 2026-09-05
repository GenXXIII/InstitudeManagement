using InstituteManagement.Application.Features.Operations;
using MediatR;

namespace InstituteManagement.Application.Features.Operations.GetOperation;

public sealed class GetOperationHandler(IOperationQueryService service) : IRequestHandler<GetOperationQuery, OperationDto>
{
    public Task<OperationDto> Handle(GetOperationQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Module, request.DepartmentId, cancellationToken);
}
