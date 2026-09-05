using InstituteManagement.Application.Features.Management.Classrooms;
using InstituteManagement.Infrastructure.Services.Catalog;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Classrooms;

public sealed class ClassroomManagementService(InstituteDbContext db, InstituteCache cache) : CatalogFeatureBase<ClassroomResponseDto>(db, cache), IClassroomManagementService
{
    public override CatalogResource Resource => CatalogResource.Classrooms;
    public override async Task<IReadOnlyList<ClassroomResponseDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var now = await InstituteLocalTime.NowAsync(Db, ct);
        var time = TimeOnly.FromDateTime(now);
        var inStudyRoomIds = (await Db.ScheduleEntries.AsNoTracking()
            .Where(entry => entry.Status != "Cancelled" && entry.DayOfWeek == now.DayOfWeek && entry.StartsAt <= time && entry.EndsAt > time)
            .Select(entry => entry.ClassroomId)
            .ToListAsync(ct)).ToHashSet();
        var rooms = await Db.Classrooms.AsNoTracking()
            .Where(room => room.Status != "Inactive")
            .ToListAsync(ct);
        return rooms.Where(room => Matches(search, room.ClassroomCode, room.Building, room.RoomType, room.Status, "Shared institute"))
            .Select(room => new ClassroomResponseDto(room.Id, new ClassroomValuesDto(
                room.ClassroomCode,
                room.Building,
                room.RoomType,
                "",
                "Shared institute",
                room.Capacity.ToString(),
                room.Status,
                inStudyRoomIds.Contains(room.Id) ? "In Study" : room.Status,
                room.DeviceOnline.ToString().ToLowerInvariant(),
                room.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<ClassroomResponseDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var classroomCode = RequiredCode(values, "classroomCode");
        await EnsureUniqueAsync(Db.Classrooms.Where(room => room.ClassroomCode == classroomCode), "ClassroomCode", ct);
        var status = RoomStatus(values); var deviceOnline = Bool(values, "deviceOnline", true); await ValidateDeviceAsync(status, deviceOnline, ct);
        var defaultCapacity = await DefaultCapacityAsync(40, ct);
        var entity = new Classroom
        {
            ClassroomCode = classroomCode,
            Building = Required(values, "building"),
            RoomType = RoomType(values),
            DepartmentId = null,
            Capacity = IntInRange(values, "capacity", defaultCapacity, 1, 10000),
            Status = status,
            DeviceOnline = deviceOnline
        };
        await AddDeviceAlertAsync(entity, !deviceOnline, ct);
        return await SaveCreatedAsync(entity, values, ct);
    }
    public override async Task<ClassroomResponseDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Classrooms, id, ct);
        var classroomCode = RequiredCode(values, "classroomCode");
        await EnsureUniqueAsync(Db.Classrooms.Where(room => room.Id != id && room.ClassroomCode == classroomCode), "ClassroomCode", ct);
        var status = RoomStatus(values);
        var wasOnline = entity.DeviceOnline;
        var deviceOnline = Bool(values, "deviceOnline", true); await ValidateDeviceAsync(status, deviceOnline, ct);
        if (status == "Inactive") await ValidateDeleteAsync(entity, ct);
        entity.ClassroomCode = classroomCode;
        entity.Building = Required(values, "building");
        entity.RoomType = RoomType(values);
        entity.DepartmentId = null;
        entity.Capacity = IntInRange(values, "capacity", entity.Capacity, 1, 10000);
        entity.Status = status;
        entity.DeviceOnline = deviceOnline;
        Touch(entity);
        await AddDeviceAlertAsync(entity, wasOnline && !deviceOnline, ct);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { if (await Db.ScheduleEntries.AnyAsync(x => x.ClassroomId == entity.Id && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Classroom is still used by an active timetable entry."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Classrooms.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { var room = (Classroom)entity; room.Status = "Inactive"; room.DeviceOnline = false; Touch(room); }
    protected override ClassroomResponseDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new ClassroomResponseDto(id, new ClassroomValuesDto(
            Get(values, "classroomCode"),
            Get(values, "building"),
            Get(values, "roomType", "Classroom"),
            "",
            "Shared institute",
            Get(values, "capacity"),
            Get(values, "status", "Available"),
            Get(values, "studyStatus", Get(values, "status", "Available")),
            Get(values, "deviceOnline", "true"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private static string RoomStatus(Dictionary<string, string> values)
        => OneOf(values, "status", "Available", "Available", "Maintenance", "Inactive");

    private static string RoomType(Dictionary<string, string> values) =>
        OneOf(values, "roomType", "Classroom", "Classroom", "Meeting Room");

    private async Task ValidateDeviceAsync(string status, bool deviceOnline, CancellationToken ct)
    {
        var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "classrooms" && x.Key == "attendanceDeviceRequired").Select(x => x.Value).FirstOrDefaultAsync(ct);
        var required = !bool.TryParse(value, out var enabled) || enabled;
        if (required && status == "Available" && !deviceOnline) throw new InvalidOperationException("An online attendance device is required for available learning spaces by Classroom settings.");
    }

    private async Task<int> DefaultCapacityAsync(int fallback, CancellationToken ct)
    {
        var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "classrooms" && x.Key == "defaultCapacity").Select(x => x.Value).FirstOrDefaultAsync(ct);
        return int.TryParse(value, out var configured) && configured is >= 1 and <= 10000 ? configured : fallback;
    }

    private async Task AddDeviceAlertAsync(Classroom room, bool shouldAlert, CancellationToken ct)
    {
        if (!shouldAlert) return;
        var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "notifications" && x.Key == "deviceAlerts").Select(x => x.Value).FirstOrDefaultAsync(ct);
        if (!bool.TryParse(value, out var enabled) || enabled)
            Db.Notifications.Add(new Notification { Title = "Classroom device offline", Message = $"Room {room.ClassroomCode} attendance device is offline.", Severity = "Warning" });
    }
}
