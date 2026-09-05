using InstituteManagement.Application.Features.History;
using InstituteManagement.Application.Features.Record;
using MediatR;

namespace InstituteManagement.Application.Features.History.GetHistory;

public sealed class GetHistoryHandler(IHistoryQueryService service) : IRequestHandler<GetHistoryQuery, IReadOnlyList<RecordDto>>
{
    public Task<IReadOnlyList<RecordDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.Type, cancellationToken);
}
