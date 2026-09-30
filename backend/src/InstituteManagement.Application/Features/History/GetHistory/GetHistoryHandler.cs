using InstituteManagement.Application.Features.History;
using MediatR;

namespace InstituteManagement.Application.Features.History.GetHistory;

public sealed class GetHistoryHandler(IHistoryQueryService service) : IRequestHandler<GetHistoryQuery, HistoryPageDto>
{
    public Task<HistoryPageDto> Handle(GetHistoryQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.Type, request.Page, cancellationToken);
}
