using InstituteManagement.API.Contracts.Attendance.ClassSessions;
using InstituteManagement.API.Routes;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers.Attendance.ClassSessions;

[ApiController]
[Route(ApiRoutes.MobileClasses)]
public sealed class ClassSessionStartsController(
    IClassSessionStartService service,
    IClassAttendanceQrService attendanceQr) : ControllerBase
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

    [HttpGet("{scheduleEntryId:guid}/attendance-qr")]
    public async Task<ActionResult<ClassAttendanceQrDto>> GetAttendanceQr(
        Guid scheduleEntryId,
        [FromQuery] Guid teacherId,
        CancellationToken cancellationToken) =>
        Ok(await attendanceQr.GenerateAsync(scheduleEntryId, teacherId, cancellationToken));

    [HttpPost("{scheduleEntryId:guid}/attendance/check-in")]
    public async Task<ActionResult<ClassAttendanceCheckInDto>> CheckIn(
        Guid scheduleEntryId,
        ClassAttendanceCheckInRequest request,
        CancellationToken cancellationToken) =>
        Ok(await attendanceQr.CheckInAsync(
            scheduleEntryId,
            request.StudentId,
            request.QrPayload,
            cancellationToken));
}
