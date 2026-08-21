using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class TeacherHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Teacher";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.Teachers.AsNoTracking().Include(x => x.Department).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.FullName, x.Status == "Inactive" ? "Inactive" : "Current", new { number = x.TeacherNumber, name = x.FullName, x.Email, photoStored = !string.IsNullOrWhiteSpace(x.PhotoDataUrl), x.DepartmentId, department = x.Department?.Name, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc })).ToList();
}
