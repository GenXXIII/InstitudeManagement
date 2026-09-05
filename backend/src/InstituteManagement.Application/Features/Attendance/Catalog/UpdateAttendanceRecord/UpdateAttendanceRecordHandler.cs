using MediatR;

namespace InstituteManagement.Application.Features.Attendance.UpdateAttendanceRecord;

public sealed class UpdateAttendanceRecordHandler(IAttendanceCatalogService service) : IRequestHandler<UpdateAttendanceRecordCommand, AttendanceResponseDto>
{
    public Task<AttendanceResponseDto> Handle(UpdateAttendanceRecordCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.Id, request.Values, cancellationToken);
}
