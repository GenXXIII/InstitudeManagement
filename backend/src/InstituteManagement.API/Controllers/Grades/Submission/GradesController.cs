using InstituteManagement.API.Contracts.Grades;
using InstituteManagement.Application.Features.Grades.SubmitGrade;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Grades;

[ApiController]
[Route(ApiRoutes.Grades)]
public sealed class GradesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitGradeRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitGradeCommand(request.StudentId, request.CourseId, request.Score), cancellationToken);
        return Accepted();
    }
}
