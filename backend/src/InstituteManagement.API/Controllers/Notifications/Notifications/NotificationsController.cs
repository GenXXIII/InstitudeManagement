using InstituteManagement.API.Contracts.Notifications.Notifications;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Notifications.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Notifications.Notifications;

[ApiController]
[Route(ApiRoutes.NotificationCenter.Notifications)]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSystem(string? search, bool unreadOnly = false, bool prioritizeUnread = false, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await service.GetSystemAsync(search, unreadOnly, prioritizeUnread, new PageRequest(page, pageSize), cancellationToken));

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
