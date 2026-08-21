namespace InstituteManagement.Application.DTOs;

public sealed record OperationDto(
    string Module,
    string Title,
    string Description,
    IReadOnlyList<MetricDto> Metrics,
    IReadOnlyList<ActivityDto> Activity,
    IReadOnlyList<ActivityDto> Attention,
    IReadOnlyList<OperationSummaryDto>? Summary = null,
    IReadOnlyList<StudentOperationDto>? Students = null,
    IReadOnlyList<TeacherOperationDto>? Teachers = null,
    IReadOnlyList<ClassroomOperationDto>? Classrooms = null,
    IReadOnlyList<CourseOperationDto>? Courses = null,
    IReadOnlyList<WeeklyTimetableSlotDto>? WeeklySchedule = null,
    IReadOnlyList<TimetablePeriodDto>? TimetablePeriods = null,
    IReadOnlyList<TimetableRoomDto>? TimetableRooms = null,
    IReadOnlyList<AttendanceOperationDto>? Attendance = null,
    IReadOnlyList<DepartmentOperationDto>? Departments = null,
    IReadOnlyList<GradeOperationDto>? Grades = null);
