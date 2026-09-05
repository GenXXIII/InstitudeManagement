using InstituteManagement.Application.Features.Record;

namespace InstituteManagement.API.Contracts.Record.Sessions;

public sealed record UpdateClassSessionRecordRequest(IReadOnlyList<ClassSessionAttendanceUpdateRequest> Students)
{
    public UpdateClassSessionRecordDto ToDto() => new(Students.Select(student => student.ToDto()).ToArray());
}
