using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Classrooms;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Management.Classrooms;

public sealed class ClassroomManagementFeature(InstituteDbContext db, InstituteCache cache) : ManagementFeatureBase(db, cache)
{
    public override string Resource => "classrooms";
    public override async Task<IReadOnlyList<IManagementItemDto>> GetAsync(string? search, Guid? departmentId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var time = TimeOnly.FromDateTime(now);
        var inStudyRoomIds = (await Db.ScheduleEntries.AsNoTracking()
            .Where(entry => entry.Status != "Cancelled" && entry.DayOfWeek == now.DayOfWeek && entry.StartsAt <= time && entry.EndsAt > time)
            .Select(entry => entry.ClassroomId)
            .ToListAsync(ct)).ToHashSet();
        var rooms = await Db.Classrooms.AsNoTracking().Include(room => room.Department)
            .Where(room => room.Status != "Inactive" && (!departmentId.HasValue || room.DepartmentId == departmentId))
            .ToListAsync(ct);
        return rooms.Where(room => Matches(search, room.ClassroomCode, room.Building, room.RoomType, room.Status, room.Department?.Name))
            .Select(room => (IManagementItemDto)new ClassroomResponseDto(room.Id, new ClassroomValuesDto(
                room.ClassroomCode,
                room.Building,
                room.RoomType,
                room.DepartmentId?.ToString() ?? "",
                room.Department?.Name ?? "Shared",
                room.Capacity.ToString(),
                room.Status,
                inStudyRoomIds.Contains(room.Id) ? "In Study" : room.Status,
                room.DeviceOnline.ToString().ToLowerInvariant(),
                room.CreateAt.ToString("yyyy-MM-dd"))))
            .ToList();
    }

    public override async Task<IManagementItemDto> CreateAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var classroomCode = Required(values, "classroomCode");
        await EnsureUniqueAsync(Db.Classrooms.Where(room => room.ClassroomCode == classroomCode), "ClassroomCode", ct);
        var status = RoomStatus(values); var deviceOnline = Bool(values, "deviceOnline", true); await ValidateDeviceAsync(status, deviceOnline, ct);
        var entity = new Classroom
        {
            ClassroomCode = classroomCode,
            Building = Required(values, "building"),
            RoomType = RoomType(values),
            DepartmentId = await RelatedIdAsync<Department>(values, "departmentId", ct),
            Capacity = IntInRange(values, "capacity", 40, 1, 10000),
            Status = status,
            DeviceOnline = deviceOnline
        };
        await AddDeviceAlertAsync(entity, !deviceOnline, ct);
        return await SaveCreatedAsync(entity, values, ct);
    }
    public override async Task<IManagementItemDto> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        var entity = await RequiredEntityAsync(Db.Classrooms, id, ct);
        var classroomCode = Required(values, "classroomCode");
        await EnsureUniqueAsync(Db.Classrooms.Where(room => room.Id != id && room.ClassroomCode == classroomCode), "ClassroomCode", ct);
        var departmentId = await RelatedIdAsync<Department>(values, "departmentId", ct);
        if (entity.DepartmentId != departmentId && await Db.ScheduleEntries.AnyAsync(x => x.ClassroomId == id && x.Course!.DepartmentId != departmentId && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Move this classroom's active timetable entries before changing department.");
        var status = RoomStatus(values);
        var wasOnline = entity.DeviceOnline;
        var deviceOnline = Bool(values, "deviceOnline", true); await ValidateDeviceAsync(status, deviceOnline, ct);
        if (status == "Inactive") await ValidateDeleteAsync(entity, ct);
        entity.ClassroomCode = classroomCode;
        entity.Building = Required(values, "building");
        entity.RoomType = RoomType(values);
        entity.DepartmentId = departmentId;
        entity.Capacity = IntInRange(values, "capacity", 40, 1, 10000);
        entity.Status = status;
        entity.DeviceOnline = deviceOnline;
        Touch(entity);
        await AddDeviceAlertAsync(entity, wasOnline && !deviceOnline, ct);
        return await SaveUpdatedAsync(id, values, ct);
    }
    protected override async Task ValidateDeleteAsync(Entity entity, CancellationToken ct) { if (await Db.ScheduleEntries.AnyAsync(x => x.ClassroomId == entity.Id && x.Status != "Cancelled", ct)) throw new InvalidOperationException("Classroom is still used by an active timetable entry."); }
    protected override async Task<Entity?> FindAsync(Guid id, CancellationToken ct) => await Db.Classrooms.FindAsync([id], ct);
    protected override void Deactivate(Entity entity) { var room = (Classroom)entity; room.Status = "Inactive"; room.DeviceOnline = false; Touch(room); }
    protected override IManagementItemDto Response(Guid id, IReadOnlyDictionary<string, string> values) =>
        new ClassroomResponseDto(id, new ClassroomValuesDto(
            Get(values, "classroomCode"),
            Get(values, "building"),
            Get(values, "roomType", "Classroom"),
            Get(values, "departmentId"),
            Get(values, "department", "Shared"),
            Get(values, "capacity"),
            Get(values, "status", "Available"),
            Get(values, "studyStatus", Get(values, "status", "Available")),
            Get(values, "deviceOnline", "true"),
            Get(values, "createAt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

    private static string RoomStatus(Dictionary<string, string> values)
    {
        var status = OneOf(values, "status", "Available", "Available", "Running", "Starting", "Offline", "Inactive");
        return status == "Running" ? "Available" : status;
    }

    private static string RoomType(Dictionary<string, string> values) =>
        OneOf(values, "roomType", "Classroom", "Classroom", "Meeting Room");

    private async Task ValidateDeviceAsync(string status, bool deviceOnline, CancellationToken ct)
    {
        var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "classrooms" && x.Key == "attendanceDeviceRequired").Select(x => x.Value).FirstOrDefaultAsync(ct);
        var required = !bool.TryParse(value, out var enabled) || enabled;
        if (required && status is "Available" or "Starting" && !deviceOnline) throw new InvalidOperationException("An online attendance device is required for active learning spaces by Classroom settings.");
    }

    private async Task AddDeviceAlertAsync(Classroom room, bool shouldAlert, CancellationToken ct)
    {
        if (!shouldAlert) return;
        var value = await Db.SystemSettings.AsNoTracking().Where(x => x.Section == "notifications" && x.Key == "deviceAlerts").Select(x => x.Value).FirstOrDefaultAsync(ct);
        if (!bool.TryParse(value, out var enabled) || enabled)
            Db.Notifications.Add(new Notification { Title = "Classroom device offline", Message = $"Room {room.ClassroomCode} attendance device is offline.", Severity = "Warning" });
    }
}
