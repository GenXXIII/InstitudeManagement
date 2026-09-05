using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal static class TimetableEnrollmentItemFactory
{
    public static EnrollmentItemDto Create(
        ScheduleEntry entry,
        Guid departmentId,
        string? departmentName,
        string enrollmentCode,
        string? status = null) =>
        Item(
            entry.Id,
            ("enrollmentCode", enrollmentCode),
            ("timetableCode", entry.TimetableCode),
            ("courseId", entry.CourseId.ToString()),
            ("courseCode", entry.Course?.CourseCode ?? "Unassigned"),
            ("course", entry.Course?.Name ?? "Unassigned"),
            ("teacherId", entry.TeacherId.ToString()),
            ("teacherCode", entry.Teacher?.TeacherCode ?? "Unassigned"),
            ("teacher", entry.Teacher?.FullName ?? "Unassigned"),
            ("classroomId", entry.ClassroomId.ToString()),
            ("classroom", entry.Classroom?.ClassroomCode ?? "Unassigned"),
            ("classroomType", entry.Classroom?.RoomType ?? "Classroom"),
            ("classroomStatus", entry.Classroom?.Status ?? "Maintenance"),
            ("departmentId", departmentId.ToString()),
            ("department", departmentName ?? "Unassigned"),
            ("yearLevel", entry.YearLevel.ToString()),
            ("dayOfWeek", entry.DayOfWeek.ToString()),
            ("startsAt", entry.StartsAt.ToString("HH:mm")),
            ("endsAt", entry.EndsAt.ToString("HH:mm")),
            ("status", status ?? entry.Status),
            ("createAt", entry.CreateAt.ToString("yyyy-MM-dd")));
}
