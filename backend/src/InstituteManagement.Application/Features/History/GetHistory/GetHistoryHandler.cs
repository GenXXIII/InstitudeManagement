using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs;
using MediatR;

namespace InstituteManagement.Application.Features.History.GetHistory;

public sealed class GetHistoryHandler(IHistoryQueryService service) : IRequestHandler<GetHistoryQuery, IReadOnlyList<RecordDto>>
{
    public Task<IReadOnlyList<RecordDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request.Search, request.Type, cancellationToken);
}
