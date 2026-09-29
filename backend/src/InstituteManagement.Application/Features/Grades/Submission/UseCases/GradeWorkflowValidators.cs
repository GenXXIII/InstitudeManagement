using InstituteManagement.Application.Common.Validation;

namespace InstituteManagement.Application.Features.Grades.UseCases;

public sealed class RequestCourseGradeSubmissionCommandValidator : IRequestValidator<RequestCourseGradeSubmissionCommand>
{
    public IEnumerable<ValidationError> Validate(RequestCourseGradeSubmissionCommand request) =>
        GradeWorkflowValidation.CourseSubmission(request.TeacherId, request.CourseId, request.Students);
}

public sealed class ReviewGradeSubmissionCommandValidator : IRequestValidator<ReviewGradeSubmissionCommand>
{
    public IEnumerable<ValidationError> Validate(ReviewGradeSubmissionCommand request)
    {
        foreach (var error in GradeWorkflowValidation.RequiredId(nameof(request.GradeId), request.GradeId))
            yield return error;
        if (request.Decision?.Trim() is not ("Approved" or "Rejected" or "ResubmitRequested"))
            yield return new(nameof(request.Decision), "Decision must be Approved, Rejected, or ResubmitRequested.");
        if (!string.Equals(request.Decision?.Trim(), "Approved", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(request.Note))
            yield return new(nameof(request.Note), "Note is required when the submission is not approved.");
    }
}

public sealed class SubmitAuthorizedGradeCommandValidator : IRequestValidator<SubmitAuthorizedGradeCommand>
{
    public IEnumerable<ValidationError> Validate(SubmitAuthorizedGradeCommand request) =>
        GradeWorkflowValidation.GradeAndTeacherIds(request.GradeId, request.TeacherId);
}

public sealed class SubmitAuthorizedCourseGradesCommandValidator : IRequestValidator<SubmitAuthorizedCourseGradesCommand>
{
    public IEnumerable<ValidationError> Validate(SubmitAuthorizedCourseGradesCommand request)
    {
        foreach (var error in GradeWorkflowValidation.GradeAndTeacherIds(request.GradeId, request.TeacherId))
            yield return error;
        foreach (var error in GradeWorkflowValidation.StudentScores(request.Students))
            yield return error;
    }
}

public sealed class RequestCourseGradeResubmissionCommandValidator : IRequestValidator<RequestCourseGradeResubmissionCommand>
{
    public IEnumerable<ValidationError> Validate(RequestCourseGradeResubmissionCommand request) =>
        GradeWorkflowValidation.GradeAndTeacherIds(request.GradeId, request.TeacherId);
}

internal static class GradeWorkflowValidation
{
    public static IEnumerable<ValidationError> CourseSubmission(
        Guid teacherId,
        Guid courseId,
        IReadOnlyList<GradeStudentScore> students)
    {
        foreach (var error in RequiredId("TeacherId", teacherId)) yield return error;
        foreach (var error in RequiredId("CourseId", courseId)) yield return error;
        foreach (var error in StudentScores(students)) yield return error;
    }

    public static IEnumerable<ValidationError> GradeAndTeacherIds(Guid gradeId, Guid teacherId)
    {
        foreach (var error in RequiredId("GradeId", gradeId)) yield return error;
        foreach (var error in RequiredId("TeacherId", teacherId)) yield return error;
    }

    public static IEnumerable<ValidationError> RequiredId(string propertyName, Guid value)
    {
        if (value == Guid.Empty)
            yield return new(propertyName, $"{propertyName} is required.");
    }

    public static IEnumerable<ValidationError> StudentScores(IReadOnlyList<GradeStudentScore> students)
    {
        if (students.Count == 0)
        {
            yield return new("Students", "At least one Student score is required.");
            yield break;
        }

        if (students.GroupBy(student => student.StudentId).Any(group => group.Count() > 1))
            yield return new("Students", "A Student cannot appear more than once.");

        for (var index = 0; index < students.Count; index++)
        {
            var student = students[index];
            if (student.StudentId == Guid.Empty)
                yield return new($"Students[{index}].StudentId", "StudentId is required.");
            if (student.AssignmentScore < 0)
                yield return new($"Students[{index}].AssignmentScore", "AssignmentScore cannot be negative.");
            if (student.MidtermScore < 0)
                yield return new($"Students[{index}].MidtermScore", "MidtermScore cannot be negative.");
            if (student.FinalExamScore < 0)
                yield return new($"Students[{index}].FinalExamScore", "FinalExamScore cannot be negative.");
        }
    }
}
