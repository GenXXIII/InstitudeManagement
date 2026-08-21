namespace InstituteManagement.Application.DTOs;

public sealed record ClassroomOperationDto(Guid Id, string Room, int Floor, string Building, int Capacity, string Device, string Status);
