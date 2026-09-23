using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;

public sealed class ClassPermissionService(InstituteDbContext db, InstituteCache cache) : IClassPermissionService
{
    public async Task<ClassPermissionRequestDto> RequestAsync(Guid studentId, DateOnly sessionDate, string reason, CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty) throw new ArgumentException("Student is required.", nameof(studentId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A permission reason is required.", nameof(reason));
        var localNow = await InstituteLocalTime.NowAsync(db, cancellationToken);
        var today = DateOnly.FromDateTime(localNow);
        if (sessionDate < today || sessionDate > today.AddDays(14)) throw new InvalidOperationException("Permission can be requested for a whole day from today through the next 14 days.");

        var student = await db.Students.FindAsync([studentId], cancellationToken) ?? throw new KeyNotFoundException("Student not found.");
        var period = await CurrentPeriodAsync(cancellationToken);
        var enrollment = await db.StudentEnrollments.AsNoTracking()
            .Where(item => item.StudentId == studentId && item.Status == "Active" && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester)
            .OrderByDescending(item => item.CreateAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The student does not have an active enrollment for the current semester.");
        var hasAssignedClass = await db.TimetableEnrollments.AsNoTracking()
            .Include(item => item.Course)
            .Include(item => item.ScheduleEntry)
            .AnyAsync(item => item.Status == "Active" && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester
                && item.YearLevel == enrollment.YearLevel && item.Course != null && item.Course.DepartmentId == enrollment.DepartmentId
                && item.ScheduleEntry != null && item.ScheduleEntry.Status != "Cancelled" && item.ScheduleEntry.Shift == enrollment.Shift, cancellationToken);
        if (!hasAssignedClass) throw new InvalidOperationException("No current class is assigned to the Student's department, year, and shift.");

        var existing = await Query().FirstOrDefaultAsync(item => item.StudentId == studentId && item.SessionDate == sessionDate, cancellationToken);
        if (existing is not null && existing.Status != "Rejected") throw new InvalidOperationException($"A {existing.Status.ToLowerInvariant()} whole-day permission request already exists for this date.");
        var entity = existing ?? new ClassPermissionRequest { StudentId = studentId, SessionDate = sessionDate };
        entity.TeacherId = null;
        entity.Teacher = null;
        entity.Reason = reason.Trim();
        entity.Status = "Pending";
        entity.RequestedAtUtc = DateTime.UtcNow;
        entity.ReviewedAtUtc = null;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        if (existing is null) db.ClassPermissionRequests.Add(entity);
        db.AuditLogs.Add(new AuditLog { ResourceId = entity.Id, Type = "Day permission", Subject = student.FullName, Action = "Requested", Details = $"{sessionDate:yyyy-MM-dd} · whole day · {reason.Trim()}" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        entity.Student = student;
        return Map(entity, enrollment.PublicId);
    }

    public async Task<IReadOnlyList<ClassPermissionRequestDto>> GetForStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(await InstituteLocalTime.NowAsync(db, cancellationToken));
        var requests = await Query().Where(item => item.StudentId == studentId && item.SessionDate >= today.AddDays(-1))
            .OrderBy(item => item.SessionDate).ToListAsync(cancellationToken);
        var publicIds = await CurrentStudentPublicIdsAsync([studentId], cancellationToken);
        return requests.Select(item => Map(item, publicIds.GetValueOrDefault(item.StudentId, ""))).ToList();
    }

    public async Task<IReadOnlyList<ClassPermissionRequestDto>> GetForTeacherAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(await InstituteLocalTime.NowAsync(db, cancellationToken));
        var studentIds = await AssignedStudentIdsAsync(teacherId, cancellationToken);
        if (studentIds.Count == 0) return [];
        var requests = await Query().Where(item => studentIds.Contains(item.StudentId) && item.SessionDate >= today && item.SessionDate <= today.AddDays(14))
            .OrderBy(item => item.Status == "Pending" ? 0 : 1).ThenBy(item => item.SessionDate).ThenBy(item => item.Student!.FullName)
            .ToListAsync(cancellationToken);
        var publicIds = await CurrentStudentPublicIdsAsync(studentIds, cancellationToken);
        return requests.Select(item => Map(item, publicIds.GetValueOrDefault(item.StudentId, ""))).ToList();
    }

    public async Task<ClassPermissionRequestDto> ReviewAsync(Guid requestId, Guid teacherId, string decision, CancellationToken cancellationToken)
    {
        if (decision is not ("Approved" or "Rejected")) throw new ArgumentException("Decision must be Approved or Rejected.", nameof(decision));
        var entity = await Query().FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken) ?? throw new KeyNotFoundException("Permission request not found.");
        var studentIds = await AssignedStudentIdsAsync(teacherId, cancellationToken);
        if (!studentIds.Contains(entity.StudentId)) throw new InvalidOperationException("Only a Teacher assigned to this Student's current cohort can review the whole-day permission request.");
        if (entity.Status != "Pending") throw new InvalidOperationException("This permission request has already been reviewed.");
        var teacher = await db.Teachers.FindAsync([teacherId], cancellationToken) ?? throw new KeyNotFoundException("Teacher not found.");
        entity.TeacherId = teacherId;
        entity.Teacher = teacher;
        entity.Status = decision;
        entity.ReviewedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        if (decision == "Approved")
        {
            var attendance = await db.AttendanceRecords.FirstOrDefaultAsync(item => item.StudentId == entity.StudentId && item.Date == entity.SessionDate, cancellationToken);
            if (attendance is null)
            {
                var period = await CurrentPeriodAsync(cancellationToken);
                attendance = new AttendanceRecord { AttendanceCode = await BusinessCodeFormatter.GenerateAsync(db, "attendance", cancellationToken), StudentId = entity.StudentId, Date = entity.SessionDate, Status = "Permission", Method = "Student whole-day permission", AcademicYear = period.AcademicYear, Term = period.Semester };
                db.AttendanceRecords.Add(attendance);
            }
            else if (attendance.Status is not ("Present" or "Late"))
            {
                attendance.Status = "Permission";
                attendance.Method = "Student whole-day permission";
                attendance.CheckedInAt = null;
                attendance.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        db.AuditLogs.Add(new AuditLog { ResourceId = entity.Id, Type = "Day permission", Subject = entity.Student?.FullName ?? "Student", Action = decision, Details = $"{entity.SessionDate:yyyy-MM-dd} · whole day · {teacher.FullName}" });
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
        var publicIds = await CurrentStudentPublicIdsAsync([entity.StudentId], cancellationToken);
        return Map(entity, publicIds.GetValueOrDefault(entity.StudentId, ""));
    }

    private async Task<List<Guid>> AssignedStudentIdsAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var period = await CurrentPeriodAsync(cancellationToken);
        var cohorts = await db.TimetableEnrollments.AsNoTracking()
            .Include(item => item.Course)
            .Include(item => item.ScheduleEntry)
            .Where(item => item.TeacherId == teacherId && item.Status == "Active" && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester
                && item.Course != null && item.ScheduleEntry != null && item.ScheduleEntry.Status != "Cancelled")
            .Select(item => new { item.Course!.DepartmentId, item.YearLevel, item.ScheduleEntry!.Shift })
            .Distinct()
            .ToListAsync(cancellationToken);
        if (cohorts.Count == 0) return [];
        var enrollments = await db.StudentEnrollments.AsNoTracking()
            .Where(item => item.Status == "Active" && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester)
            .Select(item => new { item.StudentId, item.DepartmentId, item.YearLevel, item.Shift })
            .ToListAsync(cancellationToken);
        return enrollments.Where(enrollment => cohorts.Any(cohort => cohort.DepartmentId == enrollment.DepartmentId && cohort.YearLevel == enrollment.YearLevel && cohort.Shift == enrollment.Shift))
            .Select(item => item.StudentId).Distinct().ToList();
    }

    private async Task<(string AcademicYear, string Semester)> CurrentPeriodAsync(CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings.AsNoTracking().Where(item => (item.Section == "academic-year" && item.Key == "currentYear") || (item.Section == "semester" && item.Key == "currentTerm"))
            .ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        return (values.GetValueOrDefault("academic-year:currentYear", "2026–2027"), values.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }

    private IQueryable<ClassPermissionRequest> Query() => db.ClassPermissionRequests.Include(item => item.Student).Include(item => item.Teacher);

    private async Task<Dictionary<Guid, string>> CurrentStudentPublicIdsAsync(IEnumerable<Guid> studentIds, CancellationToken cancellationToken)
    {
        var ids = studentIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var period = await CurrentPeriodAsync(cancellationToken);
        return await db.StudentEnrollments.AsNoTracking()
            .Where(item => ids.Contains(item.StudentId) && item.Status == "Active" && item.AcademicYear == period.AcademicYear && item.Semester == period.Semester)
            .ToDictionaryAsync(item => item.StudentId, item => item.PublicId, cancellationToken);
    }

    private static ClassPermissionRequestDto Map(ClassPermissionRequest item, string publicId) => new(item.Id, item.StudentId, item.Student?.FullName ?? "Student", publicId, item.TeacherId, item.Teacher?.FullName ?? "Not reviewed", item.SessionDate, item.Reason, item.Status, item.RequestedAtUtc, item.ReviewedAtUtc);
}
