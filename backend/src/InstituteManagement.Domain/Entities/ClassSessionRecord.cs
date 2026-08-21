namespace InstituteManagement.Domain.Entities;

public sealed class ClassSessionRecord : Entity
{
    public Guid ScheduleEntryId { get; set; }
    public ScheduleEntry? ScheduleEntry { get; set; }
    public DateOnly SessionDate { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public Guid ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public int YearLevel { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string ClassroomCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExcusedCount { get; set; }
    public string StudentAttendanceJson { get; set; } = "[]";
}

public sealed record SessionStudentSnapshot(Guid StudentId, string StudentNumber, string StudentName, string Status, string CheckedInAt);
