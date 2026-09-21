using InstituteManagement.API.Contracts.Attendance.ClassSessions;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Attendance.ClassSessions;

[ApiController]
[Route(ApiRoutes.MobileClasses)]
public sealed class ClassPermissionRequestsController(IClassPermissionService service) : ControllerBase
{
    [HttpPost("students/{studentId:guid}/permission-requests")]
    public async Task<ActionResult<ClassPermissionRequestDto>> Create(Guid studentId, RequestClassPermissionRequest request, CancellationToken cancellationToken) =>
        Ok(await service.RequestAsync(studentId, request.SessionDate, request.Reason, cancellationToken));

    [HttpGet("students/{studentId:guid}/permission-requests")]
    public async Task<ActionResult<IReadOnlyList<ClassPermissionRequestDto>>> GetForStudent(Guid studentId, CancellationToken cancellationToken) =>
        Ok(await service.GetForStudentAsync(studentId, cancellationToken));

    [HttpGet("teachers/{teacherId:guid}/permission-requests")]
    public async Task<ActionResult<IReadOnlyList<ClassPermissionRequestDto>>> GetForTeacher(Guid teacherId, CancellationToken cancellationToken) =>
        Ok(await service.GetForTeacherAsync(teacherId, cancellationToken));

    [HttpPut("permission-requests/{requestId:guid}/decision")]
    public async Task<ActionResult<ClassPermissionRequestDto>> Review(Guid requestId, ReviewClassPermissionRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ReviewAsync(requestId, request.TeacherId, request.Decision, cancellationToken));
}
