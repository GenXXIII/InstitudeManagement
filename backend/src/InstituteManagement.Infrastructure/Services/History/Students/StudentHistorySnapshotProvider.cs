using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class StudentHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Student";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.Students.AsNoTracking().Include(x => x.Department).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.FullName, x.Status, new { studentCode = x.StudentCode, name = x.FullName, x.Email, photoStored = !string.IsNullOrWhiteSpace(x.PhotoDataUrl), x.DepartmentId, departmentCode = x.Department?.DepartmentCode, department = x.Department?.Name, year = x.YearLevel, x.Shift, x.Status, x.CreateAt, x.UpdatedAtUtc })).ToList();
}
