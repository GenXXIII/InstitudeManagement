using MediatR;

namespace InstituteManagement.Application.Features.Results.PublishResults;

public sealed record PublishReadySemesterResultsCommand : IRequest<int>;

public sealed class PublishReadySemesterResultsHandler(IResultQueryService results)
    : IRequestHandler<PublishReadySemesterResultsCommand, int>
{
    public Task<int> Handle(PublishReadySemesterResultsCommand request, CancellationToken cancellationToken) =>
        results.PublishAllAsync(cancellationToken);
}
