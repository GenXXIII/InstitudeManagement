using MediatR;

namespace InstituteManagement.Application.Features.Attendance.CreateAttendanceRecord;

public sealed class CreateAttendanceRecordHandler(IAttendanceCatalogService service) : IRequestHandler<CreateAttendanceRecordCommand, AttendanceResponseDto>
{
    public Task<AttendanceResponseDto> Handle(CreateAttendanceRecordCommand request, CancellationToken cancellationToken) =>
        service.CreateAsync(request.Values, cancellationToken);
}
