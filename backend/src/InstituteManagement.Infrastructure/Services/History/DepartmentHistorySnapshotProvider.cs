using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class DepartmentHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Department";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.Departments.AsNoTracking().Include(x => x.HeadTeacher).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Name, x.IsActive ? "Active" : "Inactive", new { x.Code, x.Name, x.HeadTeacherId, head = x.HeadTeacher?.FullName ?? x.Head, status = x.IsActive ? "Active" : "Inactive", x.CreatedAtUtc, x.UpdatedAtUtc })).ToList();
}
