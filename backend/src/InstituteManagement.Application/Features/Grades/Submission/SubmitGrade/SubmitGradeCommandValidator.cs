using InstituteManagement.Application.Common.Validation;

namespace InstituteManagement.Application.Features.Grades.SubmitGrade;

public sealed class SubmitGradeCommandValidator : IRequestValidator<SubmitGradeCommand>
{
    public IEnumerable<ValidationError> Validate(SubmitGradeCommand request)
    {
        if (request.StudentId == Guid.Empty)
            yield return new ValidationError(nameof(request.StudentId), "StudentId is required.");

        if (request.CourseId == Guid.Empty)
            yield return new ValidationError(nameof(request.CourseId), "CourseId is required.");

        if (request.TeacherId == Guid.Empty)
            yield return new ValidationError(nameof(request.TeacherId), "TeacherId is required.");

        foreach (var error in ComponentErrors(nameof(request.AssignmentScore), request.AssignmentScore)) yield return error;
        foreach (var error in ComponentErrors(nameof(request.MidtermScore), request.MidtermScore)) yield return error;
        foreach (var error in ComponentErrors(nameof(request.FinalExamScore), request.FinalExamScore)) yield return error;
    }

    private static IEnumerable<ValidationError> ComponentErrors(string field, decimal score)
    {
        if (score is < 0 or > 100)
            yield return new ValidationError(field, $"{field} must be between 0 and its configured maximum.");
    }
}
