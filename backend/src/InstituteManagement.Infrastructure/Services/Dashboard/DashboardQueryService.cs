using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Grades;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Dashboard;

public sealed class DashboardQueryService(InstituteDbContext db, InstituteCache cache) : IDashboardQueryService
{
    public async Task<DashboardDto> GetAsync(string range, CancellationToken ct)
    {
        var reportingRange = DashboardReportingRange.Normalize(range);
        var cached = await cache.ReadDashboardAsync<DashboardDto>(reportingRange, ct);
        if (cached is not null) return cached;

        var localNow = await InstituteLocalTime.NowAsync(db, ct);
        var context = DashboardRangeContext.Create(reportingRange, localNow);
        var today = DateOnly.FromDateTime(localNow);
        var utcStart = context.Start?.ToDateTime(TimeOnly.MinValue);
        var utcEndExclusive = today.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var studentCount = await db.Students.AsNoTracking().CountAsync(student => student.Status != "Inactive", ct);
        var teacherCount = await db.Teachers.AsNoTracking().CountAsync(teacher => teacher.Status != "Inactive", ct);
        var courseCount = await db.Courses.AsNoTracking().CountAsync(course => course.IsActive, ct);
        var classroomCount = await db.Classrooms.AsNoTracking().CountAsync(classroom => classroom.Status != "Inactive", ct);
        var departmentCount = await db.Departments.AsNoTracking().CountAsync(department => department.IsActive, ct);

        var attendanceQuery = db.AttendanceRecords.AsNoTracking().Where(record => record.Date <= today);
        if (context.Start.HasValue) attendanceQuery = attendanceQuery.Where(record => record.Date >= context.Start.Value);
        var attendance = await attendanceQuery.ToListAsync(ct);

        var previousAttendance = new List<AttendanceRecord>();
        if (context.PreviousStart.HasValue && context.PreviousEnd.HasValue)
            previousAttendance = await db.AttendanceRecords.AsNoTracking()
                .Where(record => record.Date >= context.PreviousStart.Value && record.Date <= context.PreviousEnd.Value)
                .ToListAsync(ct);

        var gradeQuery = db.GradeRecords.AsNoTracking().Where(grade => grade.UpdatedAtUtc < utcEndExclusive);
        if (utcStart.HasValue) gradeQuery = gradeQuery.Where(grade => grade.UpdatedAtUtc >= utcStart.Value);
        var grades = await gradeQuery.Select(grade => grade.Score).ToListAsync(ct);

        var settings = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "academic-year" || setting.Section == "semester" || setting.Section == "grade-rules" || setting.Section == "attendance-rules")
            .ToListAsync(ct);
        var academicYear = settings.FirstOrDefault(setting => setting.Section == "academic-year" && setting.Key == "currentYear")?.Value ?? "2026–2027";
        var term = settings.FirstOrDefault(setting => setting.Section == "semester" && setting.Key == "currentTerm")?.Value ?? "Semester 1";
        var autoPercentageValue = settings.FirstOrDefault(setting => setting.Section == "attendance-rules" && setting.Key == "autoPercentage")?.Value;
        var autoPercentage = !bool.TryParse(autoPercentageValue, out var calculatePercentage) || calculatePercentage;
        var gradeScale = GradeThresholds.From(settings.Where(setting => setting.Section == "grade-rules").ToDictionary(setting => setting.Key, setting => setting.Value));

        var departments = await BuildDepartmentStatusAsync(academicYear, term, ct);
        var finance = await BuildFinanceSummaryAsync(academicYear, term, ct);
        var attendanceRate = autoPercentage ? AttendanceRate(attendance) : 0;
        var previousRate = autoPercentage ? AttendanceRate(previousAttendance) : 0;
        var attendanceChange = context.PreviousStart.HasValue ? attendanceRate - previousRate : 0;
        var averageGrade = grades.Count == 0 ? 0 : Math.Round(grades.Average(), 1);

        var result = new DashboardDto(
            context.Range,
            context.Label,
            context.Start?.ToString("yyyy-MM-dd") ?? "Beginning",
            today.ToString("yyyy-MM-dd"),
            DateTime.UtcNow,
            [
                new("Active students", studentCount.ToString("N0"), "Current institute total"),
                new("Teaching staff", teacherCount.ToString("N0"), "Current active faculty", "violet"),
                new("Active courses", courseCount.ToString("N0"), "Academic course catalog", "cyan"),
                new("Learning spaces", classroomCount.ToString("N0"), "Available classrooms", "green"),
                new("Departments", departmentCount.ToString("N0"), "Active institute structure", "amber")
            ],
            attendanceRate,
            attendanceChange,
            [
                new("Present", attendance.Count(record => record.Status == "Present").ToString("N0"), "On-time check-ins", "Present"),
                new("Late", attendance.Count(record => record.Status == "Late").ToString("N0"), "After attendance threshold", "Late"),
                new("Absent", attendance.Count(record => record.Status == "Absent").ToString("N0"), "No attendance recorded", "Absent"),
                new("Permission", attendance.Count(record => record.Status is "Excused" or "Permission").ToString("N0"), "Approved absence", "Excused")
            ],
            BuildAttendanceTrend(attendance, context, today, autoPercentage),
            departments,
            averageGrade,
            [
                new("A", Percentage(grades, gradeScale.A, 101)),
                new("B", Percentage(grades, gradeScale.B, gradeScale.A)),
                new("C", Percentage(grades, gradeScale.C, gradeScale.B)),
                new("D", Percentage(grades, gradeScale.D, gradeScale.C)),
                new("E", Percentage(grades, gradeScale.E, gradeScale.D)),
                new("F", Percentage(grades, 0, gradeScale.E))
            ],
            finance);

        await cache.WriteDashboardAsync(reportingRange, result, ct);
        return result;
    }

    private async Task<FinanceSummaryDto> BuildFinanceSummaryAsync(string academicYear, string term, CancellationToken ct)
    {
        var accounts = await db.FinancialAccounts.AsNoTracking()
            .Where(account =>
                account.AcademicYear == academicYear
                && account.Semester == term
                && account.DeclaredAtUtc.HasValue
                && account.Status != "Cancelled")
            .Select(account => new
            {
                account.Currency,
                account.Status,
                TotalDue = (account.DeclaredAmount ?? account.TuitionFee + account.OtherFee)
                    + account.AdjustmentAmount
                    + account.LatePenaltyAmount,
                TotalPaid = account.Payments
                    .Where(payment => payment.Status == "Completed")
                    .Sum(payment => (decimal?)payment.Amount) ?? 0
            })
            .ToListAsync(ct);

        var totalDue = accounts.Sum(account => Math.Max(0, account.TotalDue));
        var collected = accounts.Sum(account => account.TotalPaid);
        var outstanding = accounts.Sum(account => Math.Max(0, account.TotalDue - account.TotalPaid));
        var collectionRate = totalDue <= 0 ? 0 : Math.Round(Math.Min(100, collected * 100 / totalDue), 1);

        return new FinanceSummaryDto(
            accounts.FirstOrDefault()?.Currency ?? "USD",
            $"{academicYear} · {term}",
            totalDue,
            collected,
            outstanding,
            collectionRate,
            accounts.Count(account => account.Status == "Paid"),
            accounts.Count(account => account.Status != "Paid"));
    }

    private async Task<IReadOnlyList<StatusItemDto>> BuildDepartmentStatusAsync(string academicYear, string term, CancellationToken ct)
    {
        var departments = await db.Departments.AsNoTracking().Where(department => department.IsActive)
            .OrderBy(department => department.Name).Take(6)
            .Select(department => new { department.Id, department.DepartmentCode, department.Name, department.Head })
            .ToListAsync(ct);
        var departmentIds = departments.Select(department => department.Id).ToList();
        var students = await db.StudentEnrollments.AsNoTracking()
            .Where(enrollment => departmentIds.Contains(enrollment.DepartmentId) && enrollment.AcademicYear == academicYear && enrollment.Semester == term && enrollment.Status == "Active")
            .GroupBy(enrollment => enrollment.DepartmentId)
            .Select(group => new { DepartmentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DepartmentId, item => item.Count, ct);
        var courses = await db.CourseAssignments.AsNoTracking()
            .Where(assignment => departmentIds.Contains(assignment.DepartmentId) && assignment.AcademicYear == academicYear && assignment.Semester == term && assignment.Status == "Active")
            .GroupBy(assignment => assignment.DepartmentId)
            .Select(group => new { DepartmentId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DepartmentId, item => item.Count, ct);
        return departments.Select(department => new StatusItemDto(
            department.DepartmentCode,
            department.Name,
            $"{students.GetValueOrDefault(department.Id):N0} students · {courses.GetValueOrDefault(department.Id):N0} courses",
            department.Head ?? "Head not assigned")).ToList();
    }

    private static IReadOnlyList<ChartPointDto> BuildAttendanceTrend(List<AttendanceRecord> records, DashboardRangeContext context, DateOnly today, bool enabled)
    {
        if (!enabled) return [];
        if (context.Range == "daily")
        {
            var dayRecords = records.Where(record => record.Date == today).ToList();
            return new[] { 8, 10, 12, 14, 16, 18 }.Select(hour => new ChartPointDto(
                $"{hour:00}:00",
                dayRecords.Count == 0 ? 0 : Math.Round(dayRecords.Count(record => (record.Status is "Present" or "Late") && record.CheckedInAt.HasValue && record.CheckedInAt.Value <= new TimeOnly(hour, 0)) * 100m / dayRecords.Count, 1))).ToList();
        }
        if (context.Range == "weekly")
            return Enumerable.Range(0, 7).Select(offset => context.Start!.Value.AddDays(offset)).Where(date => date <= today)
                .Select(date => new ChartPointDto(date.ToString("ddd"), AttendanceRate(records.Where(record => record.Date == date)))).ToList();
        if (context.Range == "monthly")
            return DateBuckets(context.Start!.Value, today, 7).Select(bucket => new ChartPointDto(bucket.Start.ToString("dd MMM"), AttendanceRate(records.Where(record => record.Date >= bucket.Start && record.Date <= bucket.End)))).ToList();
        if (context.Range == "yearly")
            return Enumerable.Range(1, today.Month).Select(month => new ChartPointDto(new DateOnly(today.Year, month, 1).ToString("MMM"), AttendanceRate(records.Where(record => record.Date.Year == today.Year && record.Date.Month == month)))).ToList();
        var years = records.Select(record => record.Date.Year).Distinct().OrderBy(year => year).ToList();
        return years.Select(year => new ChartPointDto(year.ToString(), AttendanceRate(records.Where(record => record.Date.Year == year)))).ToList();
    }

    private static IEnumerable<(DateOnly Start, DateOnly End)> DateBuckets(DateOnly start, DateOnly end, int days)
    {
        for (var cursor = start; cursor <= end; cursor = cursor.AddDays(days))
            yield return (cursor, cursor.AddDays(days - 1) < end ? cursor.AddDays(days - 1) : end);
    }

    private static decimal Percentage(List<decimal> values, decimal min, decimal max) =>
        values.Count == 0 ? 0 : Math.Round(values.Count(value => value >= min && value < max) * 100m / values.Count, 1);

    private static decimal AttendanceRate(IEnumerable<AttendanceRecord> records)
    {
        var values = records.ToList();
        return values.Count == 0 ? 0 : Math.Round(values.Count(record => record.Status is "Present" or "Late") * 100m / values.Count, 1);
    }
}

internal static class DashboardReportingRange
{
    private static readonly string[] Supported = ["daily", "weekly", "monthly", "yearly", "all"];
    public static string Normalize(string? range) => Supported.Contains(range, StringComparer.OrdinalIgnoreCase) ? range!.ToLowerInvariant() : "monthly";
}

internal sealed record DashboardRangeContext(string Range, string Label, DateOnly? Start, DateOnly? PreviousStart, DateOnly? PreviousEnd)
{
    public static DashboardRangeContext Create(string range, DateTime localNow)
    {
        var today = DateOnly.FromDateTime(localNow);
        if (range == "daily") return new(range, "Daily", today, today.AddDays(-1), today.AddDays(-1));
        if (range == "weekly")
        {
            var offset = ((int)today.DayOfWeek + 6) % 7;
            var start = today.AddDays(-offset);
            return new(range, "Weekly", start, start.AddDays(-7), start.AddDays(-1));
        }
        if (range == "monthly")
        {
            var start = new DateOnly(today.Year, today.Month, 1);
            var previousEnd = start.AddDays(-1);
            return new(range, "Monthly", start, new DateOnly(previousEnd.Year, previousEnd.Month, 1), previousEnd);
        }
        if (range == "yearly")
        {
            var start = new DateOnly(today.Year, 1, 1);
            return new(range, "Yearly", start, start.AddYears(-1), start.AddDays(-1));
        }
        return new("all", "All time", null, null, null);
    }
}
