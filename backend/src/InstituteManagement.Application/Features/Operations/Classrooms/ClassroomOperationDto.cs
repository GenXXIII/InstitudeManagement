namespace InstituteManagement.Application.Features.Operations;

public sealed record ClassroomOperationDto(Guid Id, string Room, string RoomType, int Floor, string Building, int Capacity, string Device, string Status, string Course, string Teacher, string TeacherAttendance, string StatusDetail);
