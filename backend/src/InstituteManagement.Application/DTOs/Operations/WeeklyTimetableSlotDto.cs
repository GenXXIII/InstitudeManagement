namespace InstituteManagement.Application.DTOs;

public sealed record WeeklyTimetableSlotDto(Guid Id, string TimetableCode, string Day, string Session, string StartsAt, string EndsAt, string Course, string Teacher, int YearLevel, string Room, string RoomType, string Status, string TeacherAttendance, string StatusDetail);
