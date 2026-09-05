using InstituteManagement.API.Contracts.Management.Courses;
using InstituteManagement.Application.Features.Management.Courses.CreateCourse;
using InstituteManagement.Application.Features.Management.Courses.DeleteCourse;
using InstituteManagement.Application.Features.Management.Courses.GetCourses;
using InstituteManagement.Application.Features.Management.Courses.UpdateCourse;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Management.Courses;

[ApiController]
[Route(ApiRoutes.Catalog.Courses)]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCoursesQuery(search, departmentId), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CourseValuesRequest values, CancellationToken cancellationToken)
    {
        var item = await sender.Send(new CreateCourseCommand(values), cancellationToken);
        return Created($"/{ApiRoutes.Catalog.Courses}/{item.Id}", item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CourseValuesRequest values, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateCourseCommand(id, values), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new DeleteCourseCommand(id), cancellationToken) ? NoContent() : NotFound();
}
