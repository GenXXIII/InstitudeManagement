namespace InstituteManagement.Application.Features.Record;

public sealed record ClassSessionAttendanceUpdateDto(Guid StudentId, string Status, string CheckedInAt);
