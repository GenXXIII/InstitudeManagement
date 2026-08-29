using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class OperationEnrollmentPeriodService(InstituteDbContext db)
{
    public async Task<OperationEnrollmentPeriod> GetAsync(CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings.AsNoTracking()
            .Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm"))
            .ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, cancellationToken);
        return new OperationEnrollmentPeriod(
            values.GetValueOrDefault("academic-year:currentYear", "2026–2027"),
            values.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }
}

public sealed record OperationEnrollmentPeriod(string AcademicYear, string Semester);
