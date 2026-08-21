using InstituteManagement.Application.Features.Attendance.RecordAttendance;

namespace InstituteManagement.Application.Tests.Attendance;

public sealed class RecordAttendanceCommandValidatorTests
{
    [Fact]
    public void Validate_rejects_empty_student_and_unknown_status()
    {
        var errors = new RecordAttendanceCommandValidator()
            .Validate(new RecordAttendanceCommand(Guid.Empty, "Unknown"))
            .ToArray();

        Assert.Contains(errors, error => error.PropertyName == "StudentId");
        Assert.Contains(errors, error => error.PropertyName == "Status");
    }
}
