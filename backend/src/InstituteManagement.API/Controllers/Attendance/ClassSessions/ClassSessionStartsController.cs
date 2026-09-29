using InstituteManagement.API.Contracts.Attendance.ClassSessions;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Application.Features.Attendance.ClassSessions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Attendance.ClassSessions;

[ApiController]
[Route(ApiRoutes.MobileClasses)]
public sealed class ClassSessionStartsController(ISender sender) : ControllerBase
{
    [HttpPost("{scheduleEntryId:guid}/start")]
    public async Task<ActionResult<ClassSessionStartDto>> Start(
        Guid scheduleEntryId,
        StartClassRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new StartClassSessionCommand(scheduleEntryId, request.TeacherId, request.QrPayload),
            cancellationToken));

    [HttpGet("teachers/{teacherId:guid}/today")]
    public async Task<ActionResult<IReadOnlyList<ClassSessionStartDto>>> GetToday(
        Guid teacherId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTeacherClassSessionsQuery(teacherId), cancellationToken));

    [HttpGet("students/{studentId:guid}/today")]
    public async Task<ActionResult<IReadOnlyList<ClassSessionStartDto>>> GetStudentToday(
        Guid studentId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetStudentClassSessionsQuery(studentId), cancellationToken));

    [HttpPost("{scheduleEntryId:guid}/attendance/check-in")]
    public async Task<ActionResult<ClassAttendanceCheckInDto>> CheckIn(
        Guid scheduleEntryId,
        ClassAttendanceCheckInRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new CheckInClassAttendanceCommand(scheduleEntryId, request.StudentId, request.QrPayload),
            cancellationToken));

}
