using InstituteManagement.API.Contracts.Attendance;
using InstituteManagement.Application.Features.Attendance.RecordAttendance;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Attendance;

[ApiController]
[Route(ApiRoutes.Attendance)]
public sealed class AttendanceController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record(RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RecordAttendanceCommand(request.StudentId, request.Status), cancellationToken);
        return Accepted();
    }
}
