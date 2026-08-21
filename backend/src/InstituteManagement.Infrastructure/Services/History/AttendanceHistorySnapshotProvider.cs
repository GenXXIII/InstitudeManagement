using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.History.HistorySnapshotFactory;

namespace InstituteManagement.Infrastructure.Services.History;

public sealed class AttendanceHistorySnapshotProvider(InstituteDbContext db) : IHistorySnapshotProvider
{
    public string Type => "Attendance";
    public async Task<IReadOnlyList<RecordDto>> GetAsync(CancellationToken cancellationToken) =>
        (await db.AttendanceRecords.AsNoTracking().Include(x => x.Student).ThenInclude(x => x!.Department).ToListAsync(cancellationToken)).Select(x => Create(x.Id, x.UpdatedAtUtc, Type, x.Student?.FullName ?? x.Id.ToString(), "Recorded", new { x.StudentId, student = x.Student?.FullName, number = x.Student?.StudentNumber, studentStatus = x.Student?.Status, department = x.Student?.Department?.Name, x.AcademicYear, x.Term, x.Date, x.CheckedInAt, x.Status, x.Method, x.CreatedAtUtc, x.UpdatedAtUtc })).ToList();
}
