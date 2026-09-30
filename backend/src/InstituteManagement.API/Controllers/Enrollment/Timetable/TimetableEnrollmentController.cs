using InstituteManagement.API.Contracts.Enrollment.Timetable;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment.Timetable.GetTimetableEnrollments;
using InstituteManagement.Application.Features.Enrollment.Timetable.RemoveTimetableEnrollment;
using InstituteManagement.Application.Features.Enrollment.Timetable.UpdateTimetableEnrollment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Enrollment.Timetable;

[ApiController]
[Route(ApiRoutes.Enrollment.Timetable)]
public sealed class TimetableEnrollmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, int? year, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetTimetableEnrollmentsQuery(search, departmentId, year, new PageRequest(page, pageSize)), cancellationToken));

    [HttpPut("{scheduleEntryId:guid}")]
    public async Task<IActionResult> Update(Guid scheduleEntryId, TimetableEnrollmentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTimetableEnrollmentCommand(scheduleEntryId, values), cancellationToken));

    [HttpDelete("{scheduleEntryId:guid}")]
    public async Task<IActionResult> Remove(Guid scheduleEntryId, CancellationToken cancellationToken) =>
        await sender.Send(new RemoveTimetableEnrollmentCommand(scheduleEntryId), cancellationToken) ? NoContent() : NotFound();
}
