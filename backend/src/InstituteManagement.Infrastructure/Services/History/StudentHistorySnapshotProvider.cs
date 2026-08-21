using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class StudentHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Student";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.Students.AsNoTracking().Include(x => x.Department).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.FullName, x.Status, new { number = x.StudentNumber, name = x.FullName, x.Email, photoStored = !string.IsNullOrWhiteSpace(x.PhotoDataUrl), x.DepartmentId, department = x.Department?.Name, year = x.YearLevel, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc })).ToList();
}
