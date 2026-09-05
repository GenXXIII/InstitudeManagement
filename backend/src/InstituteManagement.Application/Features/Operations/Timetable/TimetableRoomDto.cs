namespace InstituteManagement.Application.Features.Operations;

public sealed record TimetableRoomDto(Guid Id, string Room, string EnrollmentCode, string RoomType, string Status);
