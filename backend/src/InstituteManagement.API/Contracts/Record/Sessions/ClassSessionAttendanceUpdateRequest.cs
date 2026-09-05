using InstituteManagement.Application.Features.Record;

namespace InstituteManagement.API.Contracts.Record.Sessions;

public sealed record ClassSessionAttendanceUpdateRequest(Guid StudentId, string Status, string CheckedInAt)
{
    public ClassSessionAttendanceUpdateDto ToDto() => new(StudentId, Status, CheckedInAt);
}
