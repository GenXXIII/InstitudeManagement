using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Notifications.History;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Notifications.History;

[ApiController]
[Route(ApiRoutes.NotificationCenter.History)]
public sealed class NotificationHistoryController(INotificationHistoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(cancellationToken));

    [HttpGet("{codeOrId}")]
    public async Task<IActionResult> Get(string codeOrId, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(codeOrId, cancellationToken));
}
