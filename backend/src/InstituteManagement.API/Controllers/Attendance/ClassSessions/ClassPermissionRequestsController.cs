using InstituteManagement.API.Contracts.Attendance.ClassSessions;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Application.Features.Attendance.ClassSessions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Attendance.ClassSessions;

[ApiController]
[Route(ApiRoutes.MobileClasses)]
public sealed class ClassPermissionRequestsController(ISender sender) : ControllerBase
{
    [HttpPost("students/{studentId:guid}/permission-requests")]
    public async Task<ActionResult<ClassPermissionRequestDto>> Create(Guid studentId, RequestClassPermissionRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RequestClassPermissionCommand(studentId, request.SessionDate, request.Reason), cancellationToken));

    [HttpGet("students/{studentId:guid}/permission-requests")]
    public async Task<ActionResult<IReadOnlyList<ClassPermissionRequestDto>>> GetForStudent(Guid studentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStudentClassPermissionsQuery(studentId), cancellationToken));

    [HttpGet("teachers/{teacherId:guid}/permission-requests")]
    public async Task<ActionResult<IReadOnlyList<ClassPermissionRequestDto>>> GetForTeacher(Guid teacherId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTeacherClassPermissionsQuery(teacherId), cancellationToken));

    [HttpPut("permission-requests/{requestId:guid}/decision")]
    public async Task<ActionResult<ClassPermissionRequestDto>> Review(Guid requestId, ReviewClassPermissionRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReviewClassPermissionCommand(requestId, request.TeacherId, request.Decision), cancellationToken));
}
