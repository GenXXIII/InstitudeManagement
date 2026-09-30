using InstituteManagement.API.Contracts.Enrollment.Courses;
using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment.Courses.GetCourseAssignments;
using InstituteManagement.Application.Features.Enrollment.Courses.RemoveCourseAssignment;
using InstituteManagement.Application.Features.Enrollment.Courses.UpdateCourseAssignment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Enrollment.Courses;

[ApiController]
[Route(ApiRoutes.Enrollment.Courses)]
public sealed class CourseAssignmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, int? year, int page = 1, int pageSize = 40, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetCourseAssignmentsQuery(search, departmentId, year, new PageRequest(page, pageSize)), cancellationToken));

    [HttpPut("{courseId:guid}")]
    public async Task<IActionResult> Update(Guid courseId, CourseAssignmentValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateCourseAssignmentCommand(courseId, values), cancellationToken));

    [HttpDelete("{courseId:guid}")]
    public async Task<IActionResult> Remove(Guid courseId, CancellationToken cancellationToken) =>
        await sender.Send(new RemoveCourseAssignmentCommand(courseId), cancellationToken) ? NoContent() : NotFound();
}
