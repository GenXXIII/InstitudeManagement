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

        if (request.Score is < 0 or > 100)
            yield return new ValidationError(nameof(request.Score), "Score must be between 0 and 100.");
    }
}
