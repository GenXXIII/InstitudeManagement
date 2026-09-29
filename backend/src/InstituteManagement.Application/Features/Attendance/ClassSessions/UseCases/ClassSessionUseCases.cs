using MediatR;

namespace InstituteManagement.Application.Features.Attendance.ClassSessions.UseCases;

public sealed record StartClassSessionCommand(Guid ScheduleEntryId, Guid TeacherId, string QrPayload)
    : IRequest<ClassSessionStartDto>;

public sealed class StartClassSessionHandler(IClassSessionStartService sessions)
    : IRequestHandler<StartClassSessionCommand, ClassSessionStartDto>
{
    public Task<ClassSessionStartDto> Handle(StartClassSessionCommand request, CancellationToken cancellationToken) =>
        sessions.StartWithAttendanceQrAsync(request.ScheduleEntryId, request.TeacherId, request.QrPayload, cancellationToken);
}

public sealed record GetTeacherClassSessionsQuery(Guid TeacherId) : IRequest<IReadOnlyList<ClassSessionStartDto>>;

public sealed class GetTeacherClassSessionsHandler(IClassSessionStartService sessions)
    : IRequestHandler<GetTeacherClassSessionsQuery, IReadOnlyList<ClassSessionStartDto>>
{
    public Task<IReadOnlyList<ClassSessionStartDto>> Handle(GetTeacherClassSessionsQuery request, CancellationToken cancellationToken) =>
        sessions.GetTodayAsync(request.TeacherId, cancellationToken);
}

public sealed record GetStudentClassSessionsQuery(Guid StudentId) : IRequest<IReadOnlyList<ClassSessionStartDto>>;

public sealed class GetStudentClassSessionsHandler(IClassSessionStartService sessions)
    : IRequestHandler<GetStudentClassSessionsQuery, IReadOnlyList<ClassSessionStartDto>>
{
    public Task<IReadOnlyList<ClassSessionStartDto>> Handle(GetStudentClassSessionsQuery request, CancellationToken cancellationToken) =>
        sessions.GetTodayForStudentAsync(request.StudentId, cancellationToken);
}

public sealed record CheckInClassAttendanceCommand(Guid ScheduleEntryId, Guid StudentId, string QrPayload)
    : IRequest<ClassAttendanceCheckInDto>;

public sealed class CheckInClassAttendanceHandler(IClassAttendanceQrService attendance)
    : IRequestHandler<CheckInClassAttendanceCommand, ClassAttendanceCheckInDto>
{
    public Task<ClassAttendanceCheckInDto> Handle(CheckInClassAttendanceCommand request, CancellationToken cancellationToken) =>
        attendance.CheckInAsync(request.ScheduleEntryId, request.StudentId, request.QrPayload, cancellationToken);
}
