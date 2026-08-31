using InstituteManagement.Application.DTOs;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class ClassroomOperationReader(InstituteDbContext db, OperationContextService contextService, OperationEnrollmentPeriodService periodService) : IOperationModuleReader
{
    public string Module => "classrooms";

    public async Task<OperationDto> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var context = await contextService.GetAsync(departmentId, cancellationToken);
        var enrollmentPeriod = await periodService.GetAsync(cancellationToken);
        var classrooms = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Classroom)
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned"
                && (!departmentId.HasValue || x.DepartmentId == null || x.DepartmentId == departmentId))
            .OrderBy(x => x.Classroom!.ClassroomCode)
            .ToListAsync(cancellationToken);
        var now = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var selection = AcademicTimetablePolicy.SelectCurrentOrNext(now);
        var shift = selection.Shift;
        var period = selection.Period;
        var roomIds = classrooms.Select(room => room.ClassroomId).ToList();
        var enrolledTimetableIds = await db.TimetableEnrollments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active")
            .Select(x => x.ScheduleEntryId)
            .ToListAsync(cancellationToken);
        var courseAssignments = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        var enrolledCourseIds = courseAssignments.Select(x => x.CourseId).ToList();
        var currentSchedules = await db.ScheduleEntries.AsNoTracking().Include(entry => entry.Course).Include(entry => entry.Teacher)
            .Where(entry => roomIds.Contains(entry.ClassroomId)
                && enrolledTimetableIds.Contains(entry.Id)
                && enrolledCourseIds.Contains(entry.CourseId)
                && entry.Status != "Cancelled"
                && entry.DayOfWeek == selection.Date.DayOfWeek
                && entry.StartsAt == period.StartsAt && entry.EndsAt == period.EndsAt)
            .ToListAsync(cancellationToken);
        if (!selection.IsRunning) currentSchedules.Clear();
        var teacherIds = currentSchedules.Select(x => x.TeacherId).Distinct().ToList();
        var teacherAssignments = await db.TeacherAssignments.AsNoTracking()
            .Where(x => teacherIds.Contains(x.TeacherId) && x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester
                && x.Status != "Removed" && x.Status != "Unassigned")
            .ToListAsync(cancellationToken);

        var rows = classrooms.Where(x => x.Classroom is not null).Select(x =>
        {
            var room = x.Classroom!;
            var schedule = currentSchedules.FirstOrDefault(item => item.ClassroomId == x.ClassroomId);
            if (schedule is null)
                return new ClassroomOperationDto(x.ClassroomId, room.ClassroomCode, room.RoomType, Floor(room.ClassroomCode), room.Building, x.Capacity, room.DeviceOnline ? "Online" : "Offline", "Available", "No course in this period", "—", "Not scheduled", "Available for an assigned course.");

            var department = courseAssignments.First(item => item.CourseId == schedule.CourseId).DepartmentId;
            var teacherAssignment = teacherAssignments
                .Where(item => item.TeacherId == schedule.TeacherId && (item.DepartmentId == department || item.DepartmentId == null))
                .OrderByDescending(item => item.DepartmentId == department)
                .FirstOrDefault();
            var attendance = TeacherPresence.Attendance(schedule.Teacher?.Status, teacherAssignment?.Status);
            var running = TeacherPresence.IsPresent(attendance) && x.Status != "Unavailable" && room.DeviceOnline;
            var detail = running
                ? $"{schedule.Course?.Name ?? "Course"} is running with {schedule.Teacher?.FullName ?? "the assigned teacher"}."
                : !TeacherPresence.IsPresent(attendance)
                    ? $"{schedule.Course?.Name ?? "Course"} is assigned, but {schedule.Teacher?.FullName ?? "the teacher"} is {attendance.ToLowerInvariant()}; the course is not running."
                    : $"{schedule.Course?.Name ?? "Course"} is assigned and the teacher is present, but the classroom or attendance device is unavailable.";
            return new ClassroomOperationDto(x.ClassroomId, room.ClassroomCode, room.RoomType, Floor(room.ClassroomCode), room.Building, x.Capacity, room.DeviceOnline ? "Online" : "Offline", running ? "In Study" : "Available", schedule.Course?.Name ?? "Course", schedule.Teacher?.FullName ?? "—", attendance, detail);
        }).OrderBy(x => x.Room).ToList();

        var runningCount = rows.Count(x => x.Status == "In Study");
        var teacherAbsentCount = rows.Count(x => x.TeacherAttendance is "Absent" or "Permission");
        var metrics = new List<MetricDto>
        {
            new("Running", runningCount.ToString(), "Teacher present", "green"),
            new("Available", (rows.Count - runningCount).ToString(), "Not running", "red"),
            new("Teacher absent", teacherAbsentCount.ToString(), "Assigned course not held", "red")
        };
        var timing = selection.IsRunning ? "current" : "next";
        return new OperationDto(Module, $"Learning-space operations · {context.Scope}", $"Room status for the {timing} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}). A room runs only when its assigned course has a present teacher.", metrics, context.Activity, context.Attention, Classrooms: rows);
    }

    private static int Floor(string code) => char.IsDigit(code.FirstOrDefault()) ? code[0] - '0' : 1;
}
