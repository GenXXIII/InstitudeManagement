using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.Record.GetOperationalRecords;

public sealed class GetOperationalRecordsHandler(IOperationalRecordQueryService service) : IRequestHandler<GetOperationalRecordsQuery, IReadOnlyList<OperationalRecordDto>>
{
    public Task<IReadOnlyList<OperationalRecordDto>> Handle(GetOperationalRecordsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Module, request.Search, request.DepartmentId, cancellationToken);
}
