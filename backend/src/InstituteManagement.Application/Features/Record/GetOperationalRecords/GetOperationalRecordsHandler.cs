using InstituteManagement.Application.Features.Record;
using InstituteManagement.Application.Common.Pagination;
using MediatR;

namespace InstituteManagement.Application.Features.Record.GetOperationalRecords;

public sealed class GetOperationalRecordsHandler(IOperationalRecordQueryService service) : IRequestHandler<GetOperationalRecordsQuery, PagedResult<OperationalRecordDto>>
{
    public Task<PagedResult<OperationalRecordDto>> Handle(GetOperationalRecordsQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Module, request.Search, request.DepartmentId, request.Year, request.Period, request.History, request.Page, cancellationToken);
}
