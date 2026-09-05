using InstituteManagement.API.Contracts.Attendance;
using InstituteManagement.Application.Features.Attendance.CreateAttendanceRecord;
using InstituteManagement.Application.Features.Attendance.DeleteAttendanceRecord;
using InstituteManagement.Application.Features.Attendance.GetAttendanceRecords;
using InstituteManagement.Application.Features.Attendance.UpdateAttendanceRecord;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Attendance;

[ApiController]
[Route(ApiRoutes.Catalog.Attendance)]
public sealed class AttendanceCatalogController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAttendanceRecordsQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(AttendanceRecordValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateAttendanceRecordCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Attendance}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AttendanceRecordValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateAttendanceRecordCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteAttendanceRecordCommand(id), cancellationToken) ? NoContent() : NotFound();
}
