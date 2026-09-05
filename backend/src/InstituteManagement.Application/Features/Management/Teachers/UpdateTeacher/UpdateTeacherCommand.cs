using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.UpdateTeacher;

public sealed record UpdateTeacherCommand(Guid Id, Dictionary<string, string> Values) : IRequest<TeacherResponseDto>;
