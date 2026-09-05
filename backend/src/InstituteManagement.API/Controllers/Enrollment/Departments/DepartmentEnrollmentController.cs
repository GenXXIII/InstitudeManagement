using InstituteManagement.Application.Features.Enrollment.Departments.GetEnrollmentDepartments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Enrollment.Departments;

[ApiController]
[Route(ApiRoutes.Enrollment.Departments)]
public sealed class DepartmentEnrollmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEnrollmentDepartmentsQuery(search, departmentId, year), cancellationToken));
}
