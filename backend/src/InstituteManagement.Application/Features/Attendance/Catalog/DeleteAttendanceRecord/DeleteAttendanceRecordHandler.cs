using MediatR;

namespace InstituteManagement.Application.Features.Attendance.DeleteAttendanceRecord;

public sealed class DeleteAttendanceRecordHandler(IAttendanceCatalogService service) : IRequestHandler<DeleteAttendanceRecordCommand, bool>
{
    public Task<bool> Handle(DeleteAttendanceRecordCommand request, CancellationToken cancellationToken) =>
        service.DeleteAsync(request.Id, cancellationToken);
}
