using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class GradeHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Grade";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.GradeRecords.AsNoTracking().Include(x => x.Student).ThenInclude(x => x!.Department).Include(x => x.Course).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Student?.FullName ?? x.GradeCode, "Recorded", new { x.GradeCode, x.StudentId, student = x.Student?.FullName, studentCode = x.Student?.StudentCode, studentShift = x.Student?.Shift, studentStatus = x.Student?.Status, departmentCode = x.Student?.Department?.DepartmentCode, department = x.Student?.Department?.Name, x.CourseId, courseCode = x.Course?.CourseCode, course = x.Course?.Name, courseStatus = x.Course?.IsActive == true ? "Active" : "Inactive", x.AcademicYear, x.Term, x.Score, grade = x.LetterGrade, x.CreateAt, x.UpdatedAtUtc })).ToList();
}
