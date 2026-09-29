using MediatR;

namespace InstituteManagement.Application.Features.Attendance.ClassSessions.UseCases;

public sealed record RequestClassPermissionCommand(Guid StudentId, DateOnly SessionDate, string Reason)
    : IRequest<ClassPermissionRequestDto>;

public sealed class RequestClassPermissionHandler(IClassPermissionService permissions)
    : IRequestHandler<RequestClassPermissionCommand, ClassPermissionRequestDto>
{
    public Task<ClassPermissionRequestDto> Handle(RequestClassPermissionCommand request, CancellationToken cancellationToken) =>
        permissions.RequestAsync(request.StudentId, request.SessionDate, request.Reason, cancellationToken);
}

public sealed record GetStudentClassPermissionsQuery(Guid StudentId)
    : IRequest<IReadOnlyList<ClassPermissionRequestDto>>;

public sealed class GetStudentClassPermissionsHandler(IClassPermissionService permissions)
    : IRequestHandler<GetStudentClassPermissionsQuery, IReadOnlyList<ClassPermissionRequestDto>>
{
    public Task<IReadOnlyList<ClassPermissionRequestDto>> Handle(GetStudentClassPermissionsQuery request, CancellationToken cancellationToken) =>
        permissions.GetForStudentAsync(request.StudentId, cancellationToken);
}

public sealed record GetTeacherClassPermissionsQuery(Guid TeacherId)
    : IRequest<IReadOnlyList<ClassPermissionRequestDto>>;

public sealed class GetTeacherClassPermissionsHandler(IClassPermissionService permissions)
    : IRequestHandler<GetTeacherClassPermissionsQuery, IReadOnlyList<ClassPermissionRequestDto>>
{
    public Task<IReadOnlyList<ClassPermissionRequestDto>> Handle(GetTeacherClassPermissionsQuery request, CancellationToken cancellationToken) =>
        permissions.GetForTeacherAsync(request.TeacherId, cancellationToken);
}

public sealed record ReviewClassPermissionCommand(Guid RequestId, Guid TeacherId, string Decision)
    : IRequest<ClassPermissionRequestDto>;

public sealed class ReviewClassPermissionHandler(IClassPermissionService permissions)
    : IRequestHandler<ReviewClassPermissionCommand, ClassPermissionRequestDto>
{
    public Task<ClassPermissionRequestDto> Handle(ReviewClassPermissionCommand request, CancellationToken cancellationToken) =>
        permissions.ReviewAsync(request.RequestId, request.TeacherId, request.Decision, cancellationToken);
}
