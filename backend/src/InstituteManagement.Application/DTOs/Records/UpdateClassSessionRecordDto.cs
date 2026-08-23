namespace InstituteManagement.Application.DTOs;

public sealed record ClassSessionAttendanceUpdateDto(Guid StudentId, string Status, string CheckedInAt);

public sealed record UpdateClassSessionRecordDto(IReadOnlyList<ClassSessionAttendanceUpdateDto> Students);
