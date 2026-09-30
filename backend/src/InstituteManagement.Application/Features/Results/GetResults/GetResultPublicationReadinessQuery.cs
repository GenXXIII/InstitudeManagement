using MediatR;

namespace InstituteManagement.Application.Features.Results.GetResults;

public sealed record GetResultPublicationReadinessQuery : IRequest<ResultPublicationReadinessDto>;

public sealed class GetResultPublicationReadinessHandler(IResultQueryService results)
    : IRequestHandler<GetResultPublicationReadinessQuery, ResultPublicationReadinessDto>
{
    public Task<ResultPublicationReadinessDto> Handle(
        GetResultPublicationReadinessQuery request,
        CancellationToken cancellationToken) =>
        results.GetPublicationReadinessAsync(cancellationToken);
}
