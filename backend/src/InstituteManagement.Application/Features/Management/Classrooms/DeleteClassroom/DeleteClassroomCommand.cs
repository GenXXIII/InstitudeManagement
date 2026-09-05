using MediatR;

namespace InstituteManagement.Application.Features.Management.Classrooms.DeleteClassroom;

public sealed record DeleteClassroomCommand(Guid Id) : IRequest<bool>;
