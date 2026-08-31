namespace InstituteManagement.Application.DTOs;

public sealed record CourseOperationDto(Guid Id, string Course, string CourseCode, string Teacher, string Department, int Capacity, string Status, string TeacherAttendance, string StatusDetail);
