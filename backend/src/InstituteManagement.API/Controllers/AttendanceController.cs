using InstituteManagement.API.Contracts;
using InstituteManagement.Application.Features.Attendance.RecordAttendance;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record(AttendanceRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RecordAttendanceCommand(request.StudentId, request.Status), cancellationToken);
        return Accepted();
    }
}
