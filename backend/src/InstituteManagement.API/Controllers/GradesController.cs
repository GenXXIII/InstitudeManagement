using InstituteManagement.API.Contracts;
using InstituteManagement.Application.Features.Grades.SubmitGrade;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[ApiController]
[Route("api/grades")]
public sealed class GradesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(GradeRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitGradeCommand(request.StudentId, request.CourseId, request.Score), cancellationToken);
        return Accepted();
    }
}
