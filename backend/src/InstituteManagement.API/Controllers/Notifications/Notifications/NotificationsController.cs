using InstituteManagement.API.Contracts.Notifications.Notifications;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Notifications.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Notifications.Notifications;

[ApiController]
[Route(ApiRoutes.NotificationCenter.Notifications)]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUnread(CancellationToken cancellationToken) =>
        Ok(await service.GetUnreadAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.MarkReadAsync(id, cancellationToken));

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken) =>
        Ok(new { markedRead = await service.MarkAllReadAsync(cancellationToken) });

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateNotificationRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, request.ToDto(), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
