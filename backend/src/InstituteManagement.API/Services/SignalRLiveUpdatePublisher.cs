using InstituteManagement.API.Hubs;
using InstituteManagement.Application.Common.LiveUpdates;
using Microsoft.AspNetCore.SignalR;

namespace InstituteManagement.API.Services;

public sealed class SignalRLiveUpdatePublisher(IHubContext<InstituteHub> hub) : ILiveUpdatePublisher
{
    public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("InstituteEvent", new { eventName, payload, occurredAt = DateTime.UtcNow }, cancellationToken);
}
