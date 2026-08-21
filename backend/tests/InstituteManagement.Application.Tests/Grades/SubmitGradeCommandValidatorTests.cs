using InstituteManagement.Application.Features.Grades.SubmitGrade;

namespace InstituteManagement.Application.Tests.Grades;

public sealed class SubmitGradeCommandValidatorTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Validate_rejects_score_outside_supported_range(decimal score)
    {
        var command = new SubmitGradeCommand(Guid.NewGuid(), Guid.NewGuid(), score);

        var errors = new SubmitGradeCommandValidator().Validate(command);

        Assert.Contains(errors, error => error.PropertyName == "Score");
    }
}
