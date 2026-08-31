using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/notification-center")]
public sealed class NotificationCenterController(INotificationCenterService service) : ControllerBase
{
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken) => Ok(await service.GetNotificationsAsync(cancellationToken));

    [HttpGet("notifications/{id:guid}")]
    public async Task<IActionResult> GetNotification(Guid id, CancellationToken cancellationToken) => Ok(await service.GetNotificationAsync(id, cancellationToken));

    [HttpPut("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken cancellationToken) => Ok(await service.MarkNotificationReadAsync(id, cancellationToken));

    [HttpPut("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken cancellationToken) =>
        Ok(new { markedRead = await service.MarkAllNotificationsReadAsync(cancellationToken) });

    [HttpPut("notifications/{id:guid}")]
    public async Task<IActionResult> UpdateNotification(Guid id, UpdateNotificationRequestDto request, CancellationToken cancellationToken) => Ok(await service.UpdateNotificationAsync(id, request, cancellationToken));

    [HttpDelete("notifications/{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken) { await service.DeleteNotificationAsync(id, cancellationToken); return NoContent(); }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken) => Ok(await service.GetAnnouncementsAsync(cancellationToken));

    [HttpPost("alerts")]
    public async Task<IActionResult> CreateAlert(AnnouncementRequestDto request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAnnouncementAsync(request, cancellationToken);
        return Created($"/api/notification-center/alerts/{item.Id}", item);
    }

    [HttpPut("alerts/{id:guid}")]
    public async Task<IActionResult> UpdateAlert(Guid id, AnnouncementRequestDto request, CancellationToken cancellationToken) => Ok(await service.UpdateAnnouncementAsync(id, request, cancellationToken));

    [HttpDelete("alerts/{id:guid}")]
    public async Task<IActionResult> DeleteAlert(Guid id, CancellationToken cancellationToken) { await service.DeleteAnnouncementAsync(id, cancellationToken); return NoContent(); }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken) => Ok(await service.GetHistoryAsync(cancellationToken));

    [HttpGet("history/{id:guid}")]
    public async Task<IActionResult> GetHistoryItem(Guid id, CancellationToken cancellationToken) => Ok(await service.GetHistoryItemAsync(id, cancellationToken));
}
