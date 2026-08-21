using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class GradeHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Grade";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.GradeRecords.AsNoTracking().Include(x => x.Student).ThenInclude(x => x!.Department).Include(x => x.Course).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Student?.FullName ?? x.Id.ToString(), "Recorded", new { x.StudentId, student = x.Student?.FullName, number = x.Student?.StudentNumber, studentStatus = x.Student?.Status, department = x.Student?.Department?.Name, x.CourseId, course = x.Course?.Name, courseStatus = x.Course?.IsActive == true ? "Active" : "Inactive", x.Score, grade = x.LetterGrade, x.Term, x.CreatedAtUtc, x.UpdatedAtUtc })).ToList();
}
