using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class AttendanceHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Attendance";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken)
    {
        var period = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "academic-year" && item.Key == "currentYear" || item.Section == "semester" && item.Key == "currentTerm")
            .ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        var currentAcademicYear = period.GetValueOrDefault("academic-year:currentYear", string.Empty);
        var currentTerm = period.GetValueOrDefault("semester:currentTerm", string.Empty);
        var publishedPeriods = (await db.SemesterResultPublications.AsNoTracking()
                .Select(item => new { item.StudentId, item.AcademicYear, item.Term })
                .ToListAsync(cancellationToken))
            .Select(item => (item.StudentId, item.AcademicYear, item.Term))
            .ToHashSet();
        var closedPaymentPeriods = (await db.FinancialAccounts.AsNoTracking()
                .Where(item => item.ClosedAtUtc.HasValue)
                .Select(item => new { item.StudentId, item.AcademicYear, item.Semester })
                .ToListAsync(cancellationToken))
            .Select(item => (item.StudentId, item.AcademicYear, item.Semester))
            .ToHashSet();
        var attendance = await db.AttendanceRecords.AsNoTracking().Include(x => x.Student).ThenInclude(x => x!.Department).ToListAsync(cancellationToken);
        return attendance.Where(item =>
                (item.AcademicYear != currentAcademicYear || item.Term != currentTerm)
                && publishedPeriods.Contains((item.StudentId, item.AcademicYear, item.Term))
                && closedPaymentPeriods.Contains((item.StudentId, item.AcademicYear, item.Term)))
            .Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Student?.FullName ?? x.AttendanceCode, "Recorded", new { x.AttendanceCode, x.StudentId, student = x.Student?.FullName, studentCode = x.Student?.StudentCode, studentShift = x.Student?.Shift, studentStatus = x.Student?.Status, departmentCode = x.Student?.Department?.DepartmentCode, department = x.Student?.Department?.Name, x.AcademicYear, x.Term, x.Date, x.CheckedInAt, x.Status, x.Method, x.CreateAt, x.UpdatedAtUtc }))
            .ToList();
    }
}
