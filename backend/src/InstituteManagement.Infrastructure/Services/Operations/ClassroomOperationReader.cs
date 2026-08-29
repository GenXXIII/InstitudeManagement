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
        var enrolledCourseIds = await db.CourseAssignments.AsNoTracking()
            .Where(x => x.AcademicYear == enrollmentPeriod.AcademicYear && x.Semester == enrollmentPeriod.Semester && x.Status == "Active"
                && (!departmentId.HasValue || x.DepartmentId == departmentId))
            .Select(x => x.CourseId)
            .ToListAsync(cancellationToken);
        var timetableRoomIds = (await db.ScheduleEntries.AsNoTracking()
            .Where(entry => roomIds.Contains(entry.ClassroomId)
                && enrolledTimetableIds.Contains(entry.Id)
                && enrolledCourseIds.Contains(entry.CourseId)
                && entry.Status != "Cancelled"
                && entry.DayOfWeek == selection.Date.DayOfWeek
                && entry.StartsAt == period.StartsAt && entry.EndsAt == period.EndsAt)
            .Select(entry => entry.ClassroomId)
            .ToListAsync(cancellationToken)).ToHashSet();
        if (!selection.IsRunning) timetableRoomIds.Clear();
        var rows = classrooms.Where(x => x.Classroom is not null)
            .Select(x => new ClassroomOperationDto(x.ClassroomId, x.Classroom!.ClassroomCode, x.Classroom.RoomType, char.IsDigit(x.Classroom.ClassroomCode.FirstOrDefault()) ? x.Classroom.ClassroomCode[0] - '0' : 1, x.Classroom.Building, x.Capacity, x.Classroom.DeviceOnline ? "Online" : "Offline", timetableRoomIds.Contains(x.ClassroomId) && x.Status != "Unavailable" ? "In Study" : x.Status))
            .OrderBy(x => x.Room)
            .ToList();
        var metrics = new List<MetricDto> { new("Enrolled", classrooms.Count.ToString(), "Assigned classrooms and meeting rooms"), new("In Study", timetableRoomIds.Count.ToString(), "Learning now", "green"), new("Available", classrooms.Count(x => x.Status == "Available" && !timetableRoomIds.Contains(x.ClassroomId)).ToString(), "Ready"), new("Unavailable", classrooms.Count(x => x.Status == "Unavailable" || x.Classroom?.DeviceOnline == false).ToString(), "Needs attention", "red") };
        var timing = selection.IsRunning ? "current" : "next";
        return new OperationDto(Module, $"Learning-space operations · {context.Scope}", $"Room status for the {timing} timetable period ({shift.Name}, {selection.Date:dddd} {period.StartsAt:HH:mm}–{period.EndsAt:HH:mm}).", metrics, context.Activity, context.Attention, Classrooms: rows);
    }
}
