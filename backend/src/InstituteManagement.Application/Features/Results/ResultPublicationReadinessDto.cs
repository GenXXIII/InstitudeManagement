namespace InstituteManagement.Application.Features.Results;

public sealed record ResultPublicationReadinessDto(
    int Total,
    int Ready,
    int Draft,
    int Published,
    bool CanPublishAll);
