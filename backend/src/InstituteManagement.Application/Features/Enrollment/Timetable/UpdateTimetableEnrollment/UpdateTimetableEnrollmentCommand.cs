using MediatR;

using InstituteManagement.Application.Features.Enrollment;

namespace InstituteManagement.Application.Features.Enrollment.Timetable.UpdateTimetableEnrollment;

public sealed record UpdateTimetableEnrollmentCommand(Guid ScheduleEntryId, Dictionary<string, string> Values)
    : IRequest<EnrollmentItemDto>;
