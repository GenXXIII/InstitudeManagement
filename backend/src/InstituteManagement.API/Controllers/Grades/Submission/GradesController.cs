using InstituteManagement.API.Contracts.Grades;
using InstituteManagement.Application.Features.Grades;
using InstituteManagement.Application.Features.Grades.UseCases;
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
        _ = request;
        await sender.Send(new AttemptIndividualGradeSubmissionCommand(), cancellationToken);
        return Accepted();
    }

    [HttpPost("course-submissions/request")]
    public async Task<IActionResult> RequestCourseSubmission(CourseGradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new RequestCourseGradeSubmissionCommand(
                request.TeacherId,
                request.CourseId,
                request.Students.Select(item => new GradeStudentScore(item.StudentId, item.AssignmentScore, item.MidtermScore, item.FinalExamScore)).ToList()),
            cancellationToken);
        return Accepted();
    }

    [HttpPut("{gradeId:guid}/review")]
    public async Task<IActionResult> Review(Guid gradeId, ReviewGradeRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ReviewGradeSubmissionCommand(gradeId, request.Decision, request.Note), cancellationToken);
        return NoContent();
    }

    [HttpPost("{gradeId:guid}/submit")]
    public async Task<IActionResult> SubmitAuthorized(Guid gradeId, RequestGradeResubmissionRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitAuthorizedGradeCommand(gradeId, request.TeacherId), cancellationToken);
        return Accepted();
    }

    [HttpPost("{gradeId:guid}/course-submit")]
    public async Task<IActionResult> SubmitAuthorizedCourse(Guid gradeId, SubmitAuthorizedCourseGradesRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(
            new SubmitAuthorizedCourseGradesCommand(
                gradeId,
                request.TeacherId,
                request.Students?.Select(item => new GradeStudentScore(item.StudentId, item.AssignmentScore, item.MidtermScore, item.FinalExamScore)).ToList() ?? []),
            cancellationToken);
        return Accepted();
    }

    [HttpPost("{gradeId:guid}/course-resubmission-request")]
    public async Task<IActionResult> RequestCourseResubmission(Guid gradeId, CourseGradeResubmissionRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RequestCourseGradeResubmissionCommand(gradeId, request.TeacherId, request.Note), cancellationToken);
        return Accepted();
    }

    [HttpPost("final-results/confirm-all")]
    public async Task<IActionResult> ConfirmReadyFinalGrades(Guid? departmentId, int? year, CancellationToken cancellationToken) =>
        Ok(new { confirmed = await sender.Send(new ConfirmReadyFinalGradesCommand(departmentId, year), cancellationToken) });
}
