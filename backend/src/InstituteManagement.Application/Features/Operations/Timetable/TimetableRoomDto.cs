namespace InstituteManagement.Application.Features.Operations;

public sealed record TimetableRoomDto(Guid Id, string Room, string OperationCode, string RoomType, string Status);
