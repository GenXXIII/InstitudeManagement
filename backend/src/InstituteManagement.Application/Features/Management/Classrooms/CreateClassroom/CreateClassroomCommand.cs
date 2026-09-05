using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.CreateClassroom;

public sealed record CreateClassroomCommand(Dictionary<string, string> Values) : IRequest<ClassroomResponseDto>;
