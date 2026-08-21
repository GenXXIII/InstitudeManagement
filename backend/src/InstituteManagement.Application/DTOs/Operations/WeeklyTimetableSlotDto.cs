namespace InstituteManagement.Application.DTOs;

public sealed record WeeklyTimetableSlotDto(Guid Id, string Day, string StartsAt, string EndsAt, string Course, string Teacher, string Room, string Status);
