using InstituteManagement.API.Routes;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Notifications.History;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Notifications.History;

[ApiController]
[Route(ApiRoutes.NotificationCenter.History)]
public sealed class NotificationHistoryController(INotificationHistoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(search, new PageRequest(page, pageSize), cancellationToken));

    [HttpGet("{codeOrId}")]
    public async Task<IActionResult> Get(string codeOrId, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(codeOrId, cancellationToken));
}
