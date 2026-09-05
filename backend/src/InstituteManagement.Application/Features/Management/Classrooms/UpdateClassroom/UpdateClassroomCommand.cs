using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.UpdateClassroom;

public sealed record UpdateClassroomCommand(Guid Id, Dictionary<string, string> Values) : IRequest<ClassroomResponseDto>;
