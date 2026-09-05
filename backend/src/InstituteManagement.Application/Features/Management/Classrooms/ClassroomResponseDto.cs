namespace InstituteManagement.Application.Features.Management.Classrooms;

public sealed record ClassroomResponseDto(Guid Id, ClassroomValuesDto Values);

public sealed record ClassroomValuesDto(
    string ClassroomCode,
    string Building,
    string RoomType,
    string DepartmentId,
    string Department,
    string Capacity,
    string Status,
    string StudyStatus,
    string DeviceOnline,
    string CreateAt);
