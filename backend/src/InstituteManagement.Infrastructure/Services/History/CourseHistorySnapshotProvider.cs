using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class CourseHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Course";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.Courses.AsNoTracking().Include(x => x.Department).Include(x => x.Teacher).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Name, x.IsActive ? "Active" : "Inactive", new { x.CourseCode, x.Name, x.DepartmentId, department = x.Department?.Name, x.TeacherId, teacher = x.Teacher?.FullName, x.Capacity, status = x.IsActive ? "Active" : "Inactive", x.CreateAt, x.UpdatedAtUtc })).ToList();
}
