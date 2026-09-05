using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Timetable.RemoveTimetableEnrollment;

public sealed class RemoveTimetableEnrollmentHandler(ITimetableEnrollmentService service)
    : IRequestHandler<RemoveTimetableEnrollmentCommand, bool>
{
    public Task<bool> Handle(RemoveTimetableEnrollmentCommand request, CancellationToken cancellationToken) =>
        service.RemoveAsync(request.ScheduleEntryId, cancellationToken);
}
