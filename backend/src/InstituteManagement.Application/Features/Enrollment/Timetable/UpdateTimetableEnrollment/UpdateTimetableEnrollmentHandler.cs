using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Timetable.UpdateTimetableEnrollment;

public sealed class UpdateTimetableEnrollmentHandler(ITimetableEnrollmentService service)
    : IRequestHandler<UpdateTimetableEnrollmentCommand, EnrollmentItemDto>
{
    public Task<EnrollmentItemDto> Handle(UpdateTimetableEnrollmentCommand request, CancellationToken cancellationToken) =>
        service.UpdateAsync(request.ScheduleEntryId, request.Values, cancellationToken);
}
