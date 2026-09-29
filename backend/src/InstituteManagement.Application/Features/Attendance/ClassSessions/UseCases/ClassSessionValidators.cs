using InstituteManagement.Application.Common.Validation;

namespace InstituteManagement.Application.Features.Attendance.ClassSessions.UseCases;

public sealed class StartClassSessionCommandValidator : IRequestValidator<StartClassSessionCommand>
{
    public IEnumerable<ValidationError> Validate(StartClassSessionCommand request)
    {
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.ScheduleEntryId), request.ScheduleEntryId)) yield return error;
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.TeacherId), request.TeacherId)) yield return error;
        if (string.IsNullOrWhiteSpace(request.QrPayload))
            yield return new(nameof(request.QrPayload), "QrPayload is required.");
    }
}

public sealed class GetTeacherClassSessionsQueryValidator : IRequestValidator<GetTeacherClassSessionsQuery>
{
    public IEnumerable<ValidationError> Validate(GetTeacherClassSessionsQuery request) =>
        ClassSessionValidation.RequiredId(nameof(request.TeacherId), request.TeacherId);
}

public sealed class GetStudentClassSessionsQueryValidator : IRequestValidator<GetStudentClassSessionsQuery>
{
    public IEnumerable<ValidationError> Validate(GetStudentClassSessionsQuery request) =>
        ClassSessionValidation.RequiredId(nameof(request.StudentId), request.StudentId);
}

public sealed class CheckInClassAttendanceCommandValidator : IRequestValidator<CheckInClassAttendanceCommand>
{
    public IEnumerable<ValidationError> Validate(CheckInClassAttendanceCommand request)
    {
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.ScheduleEntryId), request.ScheduleEntryId)) yield return error;
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.StudentId), request.StudentId)) yield return error;
        if (string.IsNullOrWhiteSpace(request.QrPayload))
            yield return new(nameof(request.QrPayload), "QrPayload is required.");
    }
}

public sealed class RequestClassPermissionCommandValidator : IRequestValidator<RequestClassPermissionCommand>
{
    public IEnumerable<ValidationError> Validate(RequestClassPermissionCommand request)
    {
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.StudentId), request.StudentId)) yield return error;
        if (request.SessionDate == default)
            yield return new(nameof(request.SessionDate), "SessionDate is required.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            yield return new(nameof(request.Reason), "Reason is required.");
    }
}

public sealed class GetStudentClassPermissionsQueryValidator : IRequestValidator<GetStudentClassPermissionsQuery>
{
    public IEnumerable<ValidationError> Validate(GetStudentClassPermissionsQuery request) =>
        ClassSessionValidation.RequiredId(nameof(request.StudentId), request.StudentId);
}

public sealed class GetTeacherClassPermissionsQueryValidator : IRequestValidator<GetTeacherClassPermissionsQuery>
{
    public IEnumerable<ValidationError> Validate(GetTeacherClassPermissionsQuery request) =>
        ClassSessionValidation.RequiredId(nameof(request.TeacherId), request.TeacherId);
}

public sealed class ReviewClassPermissionCommandValidator : IRequestValidator<ReviewClassPermissionCommand>
{
    public IEnumerable<ValidationError> Validate(ReviewClassPermissionCommand request)
    {
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.RequestId), request.RequestId)) yield return error;
        foreach (var error in ClassSessionValidation.RequiredId(nameof(request.TeacherId), request.TeacherId)) yield return error;
        if (request.Decision?.Trim() is not ("Approved" or "Rejected"))
            yield return new(nameof(request.Decision), "Decision must be Approved or Rejected.");
    }
}

internal static class ClassSessionValidation
{
    public static IEnumerable<ValidationError> RequiredId(string propertyName, Guid value)
    {
        if (value == Guid.Empty)
            yield return new(propertyName, $"{propertyName} is required.");
    }
}
