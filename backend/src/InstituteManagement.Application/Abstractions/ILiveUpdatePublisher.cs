namespace InstituteManagement.Application.Abstractions;

public interface ILiveUpdatePublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken);
}
