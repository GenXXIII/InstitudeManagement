namespace InstituteManagement.Application.Common.LiveUpdates;

public interface ILiveUpdatePublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken);
}
