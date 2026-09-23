using InstituteManagement.API.Contracts.Grades;
using InstituteManagement.Application.Features.Grades;
using Microsoft.AspNetCore.Mvc;

using InstituteManagement.API.Routes;

namespace InstituteManagement.API.Controllers.Grades;

[ApiController]
[Route(ApiRoutes.Grades)]
public sealed class GradesController(IGradeService gradeService) : ControllerBase
{
    [HttpPost]
    public IActionResult Submit(SubmitGradeRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Individual Student grade submission is not available.",
            detail: "Submit the complete Teacher-assigned course roster through the course submission workflow.");
    }

    [HttpPost("course-submissions/request")]
    public async Task<IActionResult> RequestCourseSubmission(CourseGradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        await gradeService.RequestCourseSubmissionAsync(
            request.TeacherId,
            request.CourseId,
            request.Students.Select(item => new GradeStudentScore(item.StudentId, item.AssignmentScore, item.MidtermScore, item.FinalExamScore)).ToList(),
            cancellationToken);
        return Accepted();
    }

    [HttpPut("{gradeId:guid}/review")]
    public async Task<IActionResult> Review(Guid gradeId, ReviewGradeRequest request, CancellationToken cancellationToken)
    {
        await gradeService.ReviewAsync(gradeId, request.Decision, request.Note, cancellationToken);
        return NoContent();
    }

    [HttpPost("{gradeId:guid}/submit")]
    public async Task<IActionResult> SubmitAuthorized(Guid gradeId, RequestGradeResubmissionRequest request, CancellationToken cancellationToken)
    {
        await gradeService.SubmitAuthorizedAsync(gradeId, request.TeacherId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{gradeId:guid}/course-submit")]
    public async Task<IActionResult> SubmitAuthorizedCourse(Guid gradeId, SubmitAuthorizedCourseGradesRequest request, CancellationToken cancellationToken)
    {
        await gradeService.SubmitAuthorizedCourseAsync(
            gradeId,
            request.TeacherId,
            request.Students?.Select(item => new GradeStudentScore(item.StudentId, item.AssignmentScore, item.MidtermScore, item.FinalExamScore)).ToList() ?? [],
            cancellationToken);
        return Accepted();
    }

    [HttpPost("{gradeId:guid}/course-resubmission-request")]
    public async Task<IActionResult> RequestCourseResubmission(Guid gradeId, CourseGradeResubmissionRequest request, CancellationToken cancellationToken)
    {
        await gradeService.RequestCourseResubmissionAsync(gradeId, request.TeacherId, request.Note, cancellationToken);
        return Accepted();
    }
}
