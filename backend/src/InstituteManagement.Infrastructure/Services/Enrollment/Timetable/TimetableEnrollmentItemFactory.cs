using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Domain.Entities;
using static InstituteManagement.Infrastructure.Services.Enrollment.EnrollmentItemFactory;

namespace InstituteManagement.Infrastructure.Services.Enrollment.Timetable;

internal static class TimetableEnrollmentItemFactory
{
    public static EnrollmentItemDto Create(
        ScheduleEntry entry,
        TimetableEnrollment enrollment,
        Guid? departmentId,
        string? departmentName) =>
        Item(
            entry.Id,
            ("enrollmentCode", enrollment.EnrollmentCode),
            ("timetableCode", entry.TimetableCode),
            ("courseId", enrollment.CourseId.ToString()),
            ("courseCode", enrollment.Course?.CourseCode ?? "Unassigned"),
            ("course", enrollment.Course?.Name ?? "Unassigned"),
            ("teacherId", enrollment.TeacherId.ToString()),
            ("teacherCode", enrollment.Teacher?.TeacherCode ?? "Unassigned"),
            ("teacher", enrollment.Teacher?.FullName ?? "Unassigned"),
            ("classroomId", enrollment.ClassroomId.ToString()),
            ("classroom", enrollment.Classroom?.ClassroomCode ?? "Unassigned"),
            ("building", enrollment.Classroom?.Building ?? ""),
            ("roomType", enrollment.Classroom?.RoomType ?? "Classroom"),
            ("classroomType", enrollment.Classroom?.RoomType ?? "Classroom"),
            ("capacity", enrollment.Classroom?.Capacity.ToString() ?? ""),
            ("classroomStatus", enrollment.Classroom?.Status ?? "Maintenance"),
            ("departmentId", departmentId?.ToString() ?? ""),
            ("department", departmentName ?? "All matching students"),
            ("yearLevel", enrollment.YearLevel.ToString()),
            ("shift", entry.Shift),
            ("dayOfWeek", entry.DayOfWeek.ToString()),
            ("startsAt", entry.StartsAt.ToString("HH:mm")),
            ("endsAt", entry.EndsAt.ToString("HH:mm")),
            ("status", enrollment.Status),
            ("academicYear", enrollment.AcademicYear),
            ("semester", enrollment.Semester),
            ("createAt", enrollment.CreateAt.ToString("yyyy-MM-dd")));
}
