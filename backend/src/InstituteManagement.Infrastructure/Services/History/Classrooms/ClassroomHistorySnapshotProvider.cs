using InstituteManagement.Application.Features.Record;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class ClassroomHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Classroom";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.Classrooms.AsNoTracking().Include(x => x.Department).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.ClassroomCode, x.Status == "Inactive" ? "Inactive" : "Current", new { x.ClassroomCode, x.Building, x.RoomType, x.DepartmentId, departmentCode = x.Department?.DepartmentCode, department = x.Department?.Name, x.Capacity, x.Status, x.DeviceOnline, x.CreateAt, x.UpdatedAtUtc })).ToList();
}
