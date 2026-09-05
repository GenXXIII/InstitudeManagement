using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentValueParser;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal sealed class TimetableScheduleEditor
{
    private readonly InstituteDbContext db;
    private readonly EnrollmentSettingsReader settings;

    public TimetableScheduleEditor(InstituteDbContext db, EnrollmentSettingsReader settings)
    {
        this.db = db;
        this.settings = settings;
    }

    public async Task ApplyAsync(
        ScheduleEntry entry,
        Dictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var timetableCode = Required(values, "timetableCode");
        if (await db.ScheduleEntries.AnyAsync(
                item => item.Id != entry.Id && item.TimetableCode == timetableCode,
                cancellationToken))
        {
            throw new ArgumentException("TimetableCode already exists.");
        }

        var yearLevel = Integer(values, "yearLevel", 1, 4);
        var courseId = GuidValue(values, "courseId", true)!.Value;
        var teacherId = GuidValue(values, "teacherId", true)!.Value;
        var classroomId = GuidValue(values, "classroomId", true)!.Value;
        var course = await db.Courses.FindAsync([courseId], cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");
        var teacher = await db.Teachers.FindAsync([teacherId], cancellationToken)
            ?? throw new KeyNotFoundException("Teacher not found.");
        var classroom = await db.Classrooms.FindAsync([classroomId], cancellationToken)
            ?? throw new KeyNotFoundException("Classroom not found.");
        var classroomStatus = Choice(
            values,
            "classroomStatus",
            ["Available", "Maintenance"],
            classroom.Status == "Unavailable" ? "Maintenance" : classroom.Status);
        var allowCrossDepartment = await settings.EnabledAsync(
            "departments",
            "allowCrossDepartmentTeaching",
            false,
            cancellationToken);

        if (!course.IsActive || teacher.Status == "Inactive")
        {
            throw new InvalidOperationException("Course and teacher must be active.");
        }
        if (!allowCrossDepartment
            && teacher.DepartmentId.HasValue
            && teacher.DepartmentId != course.DepartmentId)
        {
            throw new InvalidOperationException(
                "Course and teacher must comply with the Administration cross-department teaching rule.");
        }
        ValidateClassroomYear(yearLevel, classroom.ClassroomCode);
        if (classroom.Capacity < course.Capacity)
        {
            throw new InvalidOperationException(
                $"Learning space capacity ({classroom.Capacity}) must be at least the course capacity ({course.Capacity}).");
        }

        var dayOfWeek = Enum.TryParse<DayOfWeek>(Required(values, "dayOfWeek"), true, out var day)
            ? day
            : throw new ArgumentException("dayOfWeek is invalid.");
        var startsAt = TimeOnly.TryParse(Required(values, "startsAt"), out var start)
            ? start
            : throw new ArgumentException("startsAt must be a valid time.");
        var endsAt = TimeOnly.TryParse(Required(values, "endsAt"), out var end)
            ? end
            : throw new ArgumentException("endsAt must be a valid time.");
        if (endsAt <= startsAt)
        {
            throw new ArgumentException("Timetable end time must be after start time.");
        }
        if (AcademicTimetablePolicy.Find(dayOfWeek, startsAt, endsAt) is null)
        {
            throw new ArgumentException(
                "Select one of the institute's configured teaching periods for this day.");
        }
        if (await db.ScheduleEntries.AnyAsync(
                item =>
                    item.Id != entry.Id
                    && item.Status != "Cancelled"
                    && item.DayOfWeek == dayOfWeek
                    && item.StartsAt < endsAt
                    && startsAt < item.EndsAt
                    && (item.TeacherId == teacherId || item.ClassroomId == classroomId),
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Teacher or classroom is already scheduled during this time.");
        }

        entry.TimetableCode = timetableCode;
        entry.CourseId = courseId;
        entry.Course = course;
        entry.TeacherId = teacherId;
        entry.Teacher = teacher;
        entry.ClassroomId = classroomId;
        entry.Classroom = classroom;
        classroom.Status = classroomStatus;
        classroom.UpdatedAtUtc = DateTime.UtcNow;
        entry.YearLevel = yearLevel;
        entry.DayOfWeek = dayOfWeek;
        entry.StartsAt = startsAt;
        entry.EndsAt = endsAt;
        entry.Status = Choice(
            values,
            "status",
            ["Upcoming", "Running", "Completed", "Cancelled"],
            "Upcoming");
        entry.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void ValidateClassroomYear(int yearLevel, string? classroomCode)
    {
        if (yearLevel == 1 && classroomCode != "501")
        {
            throw new InvalidOperationException("Year 1 must use Classroom 501.");
        }
        if (yearLevel >= 2 && classroomCode == "501")
        {
            throw new InvalidOperationException("Classroom 501 is reserved for Year 1.");
        }
    }
}
