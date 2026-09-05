using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.DeleteTeacher;

public sealed record DeleteTeacherCommand(Guid Id) : IRequest<bool>;
