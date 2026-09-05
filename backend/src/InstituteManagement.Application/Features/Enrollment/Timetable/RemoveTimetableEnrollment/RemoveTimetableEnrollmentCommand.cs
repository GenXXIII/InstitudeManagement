using MediatR;

namespace InstituteManagement.Application.Features.Enrollment.Timetable.RemoveTimetableEnrollment;

public sealed record RemoveTimetableEnrollmentCommand(Guid ScheduleEntryId) : IRequest<bool>;
