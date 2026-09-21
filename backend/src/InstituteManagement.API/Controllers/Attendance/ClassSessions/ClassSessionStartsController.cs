using InstituteManagement.API.Contracts.Attendance.ClassSessions;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Attendance.ClassSessions;

[ApiController]
[Route(ApiRoutes.MobileClasses)]
public sealed class ClassSessionStartsController(IClassSessionStartService service) : ControllerBase
{
    [HttpPost("{scheduleEntryId:guid}/start")]
    public async Task<ActionResult<ClassSessionStartDto>> Start(
        Guid scheduleEntryId,
        StartClassRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.StartAsync(scheduleEntryId, request.TeacherId, cancellationToken));

    [HttpGet("teachers/{teacherId:guid}/today")]
    public async Task<ActionResult<IReadOnlyList<ClassSessionStartDto>>> GetToday(
        Guid teacherId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetTodayAsync(teacherId, cancellationToken));

    [HttpGet("students/{studentId:guid}/today")]
    public async Task<ActionResult<IReadOnlyList<ClassSessionStartDto>>> GetStudentToday(
        Guid studentId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetTodayForStudentAsync(studentId, cancellationToken));
}
