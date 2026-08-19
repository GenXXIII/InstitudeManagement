namespace InstituteManagement.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Department : Entity
{
    public required string Name { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Head { get; set; } = string.Empty;
    public Guid? HeadTeacherId { get; set; }
    public Teacher? HeadTeacher { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Student : Entity
{
    public required string StudentNumber { get; set; }
    public required string FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhotoDataUrl { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int YearLevel { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class Teacher : Entity
{
    public required string TeacherNumber { get; set; }
    public required string FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhotoDataUrl { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Status { get; set; } = "Available";
}

public sealed class Classroom : Entity
{
    public required string Code { get; set; }
    public string Building { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Status { get; set; } = "Available";
    public bool DeviceOnline { get; set; } = true;
}

public sealed class Course : Entity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public int Credits { get; set; } = 3;
    public int Capacity { get; set; } = 40;
    public bool IsActive { get; set; } = true;
}

public sealed class ScheduleEntry : Entity
{
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }
    public Guid TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartsAt { get; set; }
    public TimeOnly EndsAt { get; set; }
    public string Status { get; set; } = "Upcoming";
}

public sealed class AttendanceRecord : Entity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? CheckedInAt { get; set; }
    public string Status { get; set; } = "Present";
    public string Method { get; set; } = "ID Card";
}

public sealed class GradeRecord : Entity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public decimal Score { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public string Term { get; set; } = "Semester 1";
}

public sealed class AuditLog : Entity
{
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class Notification : Entity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public bool IsRead { get; set; }
}

public sealed class SystemSetting : Entity
{
    public required string Section { get; set; }
    public required string Key { get; set; }
    public string Value { get; set; } = string.Empty;
}
