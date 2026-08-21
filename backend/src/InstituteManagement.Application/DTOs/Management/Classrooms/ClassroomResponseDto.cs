namespace InstituteManagement.Application.DTOs.Management.Classrooms;

public sealed record ClassroomResponseDto(Guid Id, ClassroomValuesDto Values) : IManagementItemDto
{
    object IManagementItemDto.Values => Values;
}

public sealed record ClassroomValuesDto(
    string Code,
    string Building,
    string RoomType,
    string DepartmentId,
    string Department,
    string Capacity,
    string Status,
    string StudyStatus,
    string DeviceOnline);
