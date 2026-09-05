using InstituteManagement.Application.Common.Validation;

namespace InstituteManagement.Application.Features.Attendance.RecordAttendance;

public sealed class RecordAttendanceCommandValidator : IRequestValidator<RecordAttendanceCommand>
{
    private static readonly HashSet<string> AllowedStatuses =
        new(["Present", "Late", "Absent", "Excused"], StringComparer.OrdinalIgnoreCase);

    public IEnumerable<ValidationError> Validate(RecordAttendanceCommand request)
    {
        if (request.StudentId == Guid.Empty)
            yield return new ValidationError(nameof(request.StudentId), "StudentId is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            yield return new ValidationError(nameof(request.Status), "Status is required.");
        else if (!AllowedStatuses.Contains(request.Status))
            yield return new ValidationError(nameof(request.Status), "Status must be Present, Late, Absent, or Excused.");
    }
}
