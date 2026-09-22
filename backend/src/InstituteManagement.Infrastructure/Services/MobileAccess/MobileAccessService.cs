using InstituteManagement.Application.Features.MobileAccess;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.MobileAccess;

public sealed class MobileAccessService(InstituteDbContext db) : IMobileAccessService
{
    private const string InitialPassword = "1234";

    public async Task<MobileSessionDto?> SignInAsync(string publicId, string password, CancellationToken cancellationToken)
    {
        var normalizedPublicId = publicId.Trim().ToUpperInvariant();
        if (normalizedPublicId.Length == 0 || password != InitialPassword) return null;
        var period = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "academic-year" && item.Key == "currentYear" || item.Section == "semester" && item.Key == "currentTerm")
            .ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", "2026–2027");
        var semester = period.GetValueOrDefault("semester:currentTerm", "Semester 1");

        var teacher = await db.TeacherAssignments.AsNoTracking()
            .Where(item => item.Status == "Assigned" && item.PublicId == normalizedPublicId
                && item.AcademicYear == academicYear && item.Semester == semester
                && item.Teacher != null && item.Teacher.Status != "Inactive")
            .Select(item => new MobileSessionDto("teacher", item.PublicId, item.TeacherId))
            .SingleOrDefaultAsync(cancellationToken);
        if (teacher is not null) return teacher;

        return await db.StudentEnrollments.AsNoTracking()
            .Where(item => item.Status == "Active" && item.PublicId == normalizedPublicId
                && item.AcademicYear == academicYear && item.Semester == semester
                && item.Student != null && item.Student.Status != "Inactive")
            .Select(item => new MobileSessionDto("student", item.PublicId, item.StudentId))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
