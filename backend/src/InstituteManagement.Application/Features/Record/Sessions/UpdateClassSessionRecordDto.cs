namespace InstituteManagement.Application.Features.Record;

public sealed record UpdateClassSessionRecordDto(IReadOnlyList<ClassSessionAttendanceUpdateDto> Students);
