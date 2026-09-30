using InstituteManagement.API.Contracts.Notifications.Announcements;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Notifications.Announcements;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Notifications.Announcements;

[ApiController]
[Route(ApiRoutes.NotificationCenter.Announcements)]
public sealed class AnnouncementsController(IAnnouncementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, bool unreadOnly = false, bool prioritizeUnread = false, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(search, unreadOnly, prioritizeUnread, new PageRequest(page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(AnnouncementRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(request.ToDto(), cancellationToken);
        return Created($"/{ApiRoutes.NotificationCenter.Announcements}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AnnouncementRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, request.ToDto(), cancellationToken));

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.MarkReadAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
