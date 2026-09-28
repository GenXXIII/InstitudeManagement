using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class StudentHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Student";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken)
    {
        var students = await db.Students.AsNoTracking().Include(x => x.Department).ToListAsync(cancellationToken);
        var studentIds = students.Select(student => student.Id).ToList();
        var enrollments = (await db.StudentEnrollments.AsNoTracking()
                .Where(enrollment => studentIds.Contains(enrollment.StudentId))
                .ToListAsync(cancellationToken))
            .GroupBy(enrollment => enrollment.StudentId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(enrollment => enrollment.AcademicYear).ThenByDescending(enrollment => enrollment.Semester).First());
        var format = await BusinessCodeFormatter.LoadAsync(db, cancellationToken);
        return students.Select(student =>
        {
            enrollments.TryGetValue(student.Id, out var enrollment);
            var historyCode = enrollment is null
                ? format.Derive(student.StudentCode, "student", "history")
                : format.PeriodLinked(enrollment.EnrollmentCode, "student", "history", enrollment.YearLevel, enrollment.Semester);
            return Create(student.Id, student.UpdatedAtUtc, Type, student.FullName, student.Status, new { studentCode = student.StudentCode, historyCode, name = student.FullName, student.Email, photoStored = !string.IsNullOrWhiteSpace(student.PhotoDataUrl), student.DepartmentId, departmentCode = student.Department?.DepartmentCode, department = student.Department?.Name, year = student.YearLevel, student.Shift, student.Status, student.CreateAt, student.UpdatedAtUtc });
        }).ToList();
    }
}
