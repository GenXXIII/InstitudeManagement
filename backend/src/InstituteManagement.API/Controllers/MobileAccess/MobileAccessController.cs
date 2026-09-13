using InstituteManagement.API.Contracts.MobileAccess;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.MobileAccess;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.MobileAccess;

[ApiController]
[Route(ApiRoutes.MobileAccess)]
public sealed class MobileAccessController(IMobileAccessService mobileAccess) : ControllerBase
{
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(MobileSignInRequest request, CancellationToken cancellationToken)
    {
        var session = await mobileAccess.SignInAsync(request.PublicId, request.Password, cancellationToken);
        return session is null
            ? Unauthorized(new ProblemDetails { Title = "Public ID or password is incorrect." })
            : Ok(session);
    }
}
