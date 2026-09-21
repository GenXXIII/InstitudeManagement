using InstituteManagement.API.Contracts.Grades;
using InstituteManagement.Application.Features.Grades.SubmitGrade;
using InstituteManagement.Application.Features.Grades;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Grades;

[ApiController]
[Route(ApiRoutes.Grades)]
public sealed class GradesController(ISender sender, IGradeService gradeService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitGradeRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitGradeCommand(request.StudentId, request.CourseId, request.TeacherId, request.AssignmentScore, request.MidtermScore, request.FinalExamScore), cancellationToken);
        return Accepted();
    }

    [HttpPut("{gradeId:guid}/review")]
    public async Task<IActionResult> Review(Guid gradeId, ReviewGradeRequest request, CancellationToken cancellationToken)
    {
        await gradeService.ReviewAsync(gradeId, request.Decision, request.Note, cancellationToken);
        return NoContent();
    }

    [HttpPost("{gradeId:guid}/resubmission-request")]
    public async Task<IActionResult> RequestResubmission(Guid gradeId, RequestGradeResubmissionRequest request, CancellationToken cancellationToken)
    {
        await gradeService.RequestResubmissionAsync(gradeId, request.TeacherId, cancellationToken);
        return Accepted();
    }

    [HttpPut("{gradeId:guid}/resubmission-permission")]
    public async Task<IActionResult> AuthorizeResubmission(Guid gradeId, CancellationToken cancellationToken)
    {
        await gradeService.AuthorizeResubmissionAsync(gradeId, cancellationToken);
        return NoContent();
    }
}
