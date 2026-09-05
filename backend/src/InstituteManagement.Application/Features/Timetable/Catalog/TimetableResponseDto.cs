namespace InstituteManagement.Application.Features.Timetable;

public sealed record TimetableResponseDto(Guid Id, TimetableValuesDto Values);

public sealed record TimetableValuesDto(
    string TimetableCode,
    string CourseId,
    string CourseCode,
    string Course,
    string TeacherId,
    string TeacherCode,
    string Teacher,
    string ClassroomId,
    string Classroom,
    string ClassroomType,
    string DepartmentId,
    string Department,
    string YearLevel,
    string DayOfWeek,
    string StartsAt,
    string EndsAt,
    string Status,
    string CreateAt);
