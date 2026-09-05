namespace InstituteManagement.Application.Features.Operations;

public sealed record CourseOperationDto(Guid Id, string Course, string CourseCode, string EnrollmentCode, string Teacher, string Department, int Capacity, string Status, string TeacherAttendance, string StatusDetail);
