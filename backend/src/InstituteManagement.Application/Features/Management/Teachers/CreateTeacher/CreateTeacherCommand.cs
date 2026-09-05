using MediatR;

namespace InstituteManagement.Application.Features.Management.Teachers.CreateTeacher;

public sealed record CreateTeacherCommand(Dictionary<string, string> Values) : IRequest<TeacherResponseDto>;
