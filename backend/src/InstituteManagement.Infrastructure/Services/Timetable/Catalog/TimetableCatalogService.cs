using InstituteManagement.Application.Features.Timetable;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Timetable;

public sealed class TimetableCatalogService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<TimetableResponseDto>(db, cache), ITimetableCatalogService
{
    public override CatalogResource Resource => CatalogResource.Timetable;

    public override async Task<IReadOnlyList<TimetableResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        _ = departmentId;
        var entries = await Db.ScheduleEntries.AsNoTracking()
            .Where(entry => entry.Status != "Cancelled")
            .ToListAsync(ct);
        return entries
            .Where(entry => Matches(search, entry.TimetableCode, entry.Shift, entry.DayOfWeek.ToString(), entry.StartsAt.ToString("HH:mm"), entry.EndsAt.ToString("HH:mm"), entry.Status))
            .Select(entry => Response(entry.Id, new Dictionary<string, string>
            {
                ["timetableCode"] = entry.TimetableCode,
                ["shift"] = entry.Shift,
                ["dayOfWeek"] = entry.DayOfWeek.ToString(),
                ["startsAt"] = entry.StartsAt.ToString("HH:mm"),
                ["endsAt"] = entry.EndsAt.ToString("HH:mm"),
                ["status"] = entry.Status,
                ["createAt"] = entry.CreateAt.ToString("yyyy-MM-dd")
            }))
            .ToList();
    }

    public override async Task<TimetableResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = new ScheduleEntry();
        await ApplyAsync(entity, values, true, ct);
        return await SaveCreatedAsync(entity, values, ct);
    }

    public override async Task<TimetableResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.ScheduleEntries, id, ct);
        await ApplyAsync(entity, values, false, ct);
        Touch(entity);
        return await SaveUpdatedAsync(id, values, ct);
    }

    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.ScheduleEntries.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { ((ScheduleEntry)entity).Status = "Cancelled"; Touch(entity); }

    protected override TimetableResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new(id, new TimetableValuesDto(
            Get(values, "timetableCode"),
            "", "", "", "", "", "", "", "", "", "", "", "",
            Get(values, "shift", AcademicTimetablePolicy.DefaultShiftName),
            Get(values, "dayOfWeek"),
            Get(values, "startsAt"),
            Get(values, "endsAt"),
            Get(values, "status", "Upcoming"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private async Task ApplyAsync(ScheduleEntry entry, Dictionary<string, string> values, bool creating, CancellationToken ct)
    {
        if (creating)
        {
            entry.TimetableCode = await ConfiguredCodeAsync(values, "timetableCode", "timetable", ct);
            await EnsureUniqueCodeAsync(Db.ScheduleEntries.Select(item => item.TimetableCode), entry.TimetableCode, "TimetableCode", ct);
        }
        values["timetableCode"] = entry.TimetableCode;
        entry.DayOfWeek = Enum.TryParse<DayOfWeek>(Required(values, "dayOfWeek"), true, out var day)
            ? day
            : throw new ArgumentException("dayOfWeek is invalid.");
        entry.StartsAt = TimeOnly.TryParse(Required(values, "startsAt"), out var startsAt)
            ? startsAt
            : throw new ArgumentException("startsAt must be a valid time.");
        entry.EndsAt = TimeOnly.TryParse(Required(values, "endsAt"), out var endsAt)
            ? endsAt
            : throw new ArgumentException("endsAt must be a valid time.");
        if (entry.EndsAt <= entry.StartsAt) throw new ArgumentException("Schedule end time must be after start time.");
        var shift = AcademicTimetablePolicy.FindShift(entry.DayOfWeek, entry.StartsAt, entry.EndsAt)
            ?? throw new ArgumentException("Select one of the institute's configured teaching periods for this day.");
        var requestedShift = OneOf(values, "shift", shift.Name, AcademicTimetablePolicy.ShiftNames.ToArray());
        if (!requestedShift.Equals(shift.Name, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Shift must match the selected day and teaching period.");
        entry.Shift = shift.Name;
        values["shift"] = entry.Shift;
        entry.Status = OneOf(values, "status", "Upcoming", "Upcoming", "Running", "Completed", "Cancelled");
    }
}
