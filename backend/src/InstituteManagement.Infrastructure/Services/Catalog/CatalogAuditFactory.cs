using System.Text.Json;
using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Services.Catalog;

internal static class CatalogAuditFactory
{
    public static AuditLog ForValues(
        CatalogResource resource,
        Guid id,
        Dictionary<string, string> values,
        string action) =>
        new()
        {
            ResourceId = id,
            Type = ResourceType(resource),
            Subject = values.GetValueOrDefault("name", ResourceDisplayId(resource, values)),
            Action = action,
            Details = JsonSerializer.Serialize(values)
        };

    public static AuditLog ForEntity(
        CatalogResource resource,
        Entity entity,
        string subject,
        string action) =>
        new()
        {
            ResourceId = entity.Id,
            Type = ResourceType(resource),
            Subject = subject,
            Action = action,
            Details = JsonSerializer.Serialize(Snapshot(entity))
        };

    public static string Subject(Entity entity) => entity switch
    {
        Student student => student.FullName,
        Teacher teacher => teacher.FullName,
        Classroom classroom => classroom.ClassroomCode,
        Course course => course.Name,
        Department department => department.Name,
        ScheduleEntry schedule => schedule.TimetableCode,
        AttendanceRecord attendance => attendance.AttendanceCode,
        GradeRecord grade => grade.GradeCode,
        _ => entity.Id.ToString()
    };

    private static string ResourceDisplayId(CatalogResource resource, IReadOnlyDictionary<string, string> values)
    {
        var key = resource switch
        {
            CatalogResource.Students => "studentCode",
            CatalogResource.Teachers => "teacherCode",
            CatalogResource.Departments => "departmentCode",
            CatalogResource.Courses => "courseCode",
            CatalogResource.Classrooms => "classroomCode",
            CatalogResource.Timetable => "timetableCode",
            CatalogResource.Attendance => "attendanceCode",
            CatalogResource.Grades => "gradeCode",
            _ => ""
        };

        var fallback = resource.ToString().ToLowerInvariant();
        return string.IsNullOrEmpty(key) ? fallback : values.GetValueOrDefault(key, fallback);
    }

    private static string ResourceType(CatalogResource resource) => resource switch
    {
        CatalogResource.Timetable => "Timetable",
        CatalogResource.Attendance => "Attendance",
        CatalogResource.Grades => "Grade",
        _ => resource.ToString().TrimEnd('s')
    };

    private static object Snapshot(Entity entity) => entity switch
    {
        Student student => new { student.StudentCode, student.FullName, student.Email, student.DepartmentId, student.YearLevel, student.Shift, student.Status },
        Teacher teacher => new { teacher.TeacherCode, teacher.FullName, teacher.Email, teacher.DepartmentId, teacher.Status },
        Classroom classroom => new { classroom.ClassroomCode, classroom.Building, classroom.RoomType, classroom.DepartmentId, classroom.Capacity, classroom.Status, classroom.DeviceOnline },
        Course course => new { course.CourseCode, course.Name, course.DepartmentId, course.TeacherId, course.Capacity, course.IsActive },
        Department department => new { department.DepartmentCode, department.Name, department.HeadTeacherId, department.IsActive },
        ScheduleEntry schedule => new { schedule.TimetableCode, schedule.CourseId, schedule.TeacherId, schedule.ClassroomId, schedule.YearLevel, schedule.DayOfWeek, schedule.StartsAt, schedule.EndsAt, schedule.Status },
        AttendanceRecord attendance => new { attendance.AttendanceCode, attendance.StudentId, attendance.Date, attendance.CheckedInAt, attendance.Status, attendance.Method },
        GradeRecord grade => new { grade.GradeCode, grade.StudentId, grade.CourseId, grade.Score, grade.LetterGrade, grade.Term },
        _ => new { entity.Id }
    };
}
