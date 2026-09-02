using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public sealed class InstituteDbContext(DbContextOptions<InstituteDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();
    public DbSet<ClassSessionRecord> ClassSessionRecords => Set<ClassSessionRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();
    public DbSet<ClassroomAssignment> ClassroomAssignments => Set<ClassroomAssignment>();
    public DbSet<TimetableEnrollment> TimetableEnrollments => Set<TimetableEnrollment>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var format = RequiresNotificationCodeFormat() ? NotificationCodeFormat() : null;
        AssignSourceBusinessCodes(format);
        CaptureNotificationHistory();
        AssignHistoryBusinessCodes(format);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var format = RequiresNotificationCodeFormat() ? NotificationCodeFormat() : null;
        AssignSourceBusinessCodes(format);
        CaptureNotificationHistory();
        AssignHistoryBusinessCodes(format);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InstituteDbContext).Assembly);
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private void CaptureNotificationHistory()
    {
        var history = new List<NotificationHistory>();
        foreach (var entry in ChangeTracker.Entries<Notification>().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var item = entry.Entity;
            var action = entry.State == EntityState.Added ? "Created" : entry.State == EntityState.Deleted ? "Removed" : entry.Property(x => x.IsRead).IsModified && item.IsRead ? "Read" : "Updated";
            history.Add(new NotificationHistory { SourceId = item.Id, SourceCode = item.NotificationCode, Kind = "Notification", Type = item.Type, Title = item.Title, Message = item.Message, Action = action });
        }
        foreach (var entry in ChangeTracker.Entries<Announcement>().Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var item = entry.Entity;
            var action = entry.State == EntityState.Added ? "Announced" : entry.State == EntityState.Deleted || !item.IsActive ? "Removed" : "Updated";
            history.Add(new NotificationHistory { SourceId = item.Id, SourceCode = item.AnnouncementCode, Kind = "Alert", Type = item.Type, Title = item.Title, Message = item.Message, Action = action });
        }
        if (history.Count > 0) NotificationHistory.AddRange(history);
    }

    private void AssignSourceBusinessCodes(NotificationCodeFormat? format)
    {
        if (format is null) return;
        var notifications = ChangeTracker.Entries<Notification>().Where(entry => entry.State == EntityState.Added).OrderBy(entry => entry.Entity.CreateAt).ThenBy(entry => entry.Entity.Id).ToList();
        if (notifications.Count > 0)
        {
            var stem = format.Stem(format.NotificationPrefix);
            var next = NextSequence(Notifications.AsNoTracking().Select(item => item.NotificationCode).ToList(), stem, format.StartingNumber);
            foreach (var entry in notifications) entry.Entity.NotificationCode = BusinessCode(stem, next++, format.PaddingWidth);
        }

        var announcements = ChangeTracker.Entries<Announcement>().Where(entry => entry.State == EntityState.Added).OrderBy(entry => entry.Entity.CreateAt).ThenBy(entry => entry.Entity.Id).ToList();
        if (announcements.Count > 0)
        {
            var stem = format.Stem(format.AnnouncementPrefix);
            var next = NextSequence(Announcements.AsNoTracking().Select(item => item.AnnouncementCode).ToList(), stem, format.StartingNumber);
            foreach (var entry in announcements) entry.Entity.AnnouncementCode = BusinessCode(stem, next++, format.PaddingWidth);
        }
    }

    private void AssignHistoryBusinessCodes(NotificationCodeFormat? format)
    {
        if (format is null) return;
        var entries = ChangeTracker.Entries<NotificationHistory>().Where(entry => entry.State == EntityState.Added).OrderBy(entry => entry.Entity.CreateAt).ThenBy(entry => entry.Entity.Id).ToList();
        if (entries.Count == 0) return;
        var stem = format.Stem(format.HistoryPrefix);
        var next = NextSequence(NotificationHistory.AsNoTracking().Select(item => item.NotificationHistoryCode).ToList(), stem, format.StartingNumber);
        foreach (var entry in entries) entry.Entity.NotificationHistoryCode = BusinessCode(stem, next++, format.PaddingWidth);
    }

    private bool RequiresNotificationCodeFormat() =>
        ChangeTracker.Entries<Notification>().Any(entry => entry.State == EntityState.Added)
        || ChangeTracker.Entries<Announcement>().Any(entry => entry.State == EntityState.Added)
        || ChangeTracker.Entries<NotificationHistory>().Any(entry => entry.State == EntityState.Added);

    private NotificationCodeFormat NotificationCodeFormat()
    {
        var values = SystemSettings.AsNoTracking().Where(setting => setting.Section == "notifications")
            .ToDictionary(setting => setting.Key, setting => setting.Value, StringComparer.OrdinalIgnoreCase);
        var timeZoneId = SystemSettings.AsNoTracking().Where(setting => setting.Section == "system" && setting.Key == "timeZone").Select(setting => setting.Value).FirstOrDefault() ?? "Asia/Phnom_Penh";
        var localNow = DateTime.UtcNow;
        try { localNow = TimeZoneInfo.ConvertTimeFromUtc(localNow, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        var separator = values.GetValueOrDefault("codeSeparator", "-");
        var width = int.TryParse(values.GetValueOrDefault("codePaddingWidth"), out var configuredWidth) ? Math.Clamp(configuredWidth, 1, 12) : 8;
        var start = long.TryParse(values.GetValueOrDefault("codeStartingNumber"), out var configuredStart) && configuredStart >= 0 ? configuredStart : 1;
        var includeYear = bool.TryParse(values.GetValueOrDefault("codeIncludeYear"), out var configuredIncludeYear) && configuredIncludeYear;
        return new(
            Prefix(values, "notificationCodePrefix", "NOT"),
            Prefix(values, "announcementCodePrefix", "ANN"),
            Prefix(values, "historyCodePrefix", "NHS"),
            separator,
            includeYear,
            localNow.Year,
            start,
            width);
    }

    private static string Prefix(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
        string.IsNullOrWhiteSpace(values.GetValueOrDefault(key)) ? fallback : values[key].Trim().ToUpperInvariant();

    private static long NextSequence(IEnumerable<string> codes, string stem, long startingNumber) => codes
        .Where(code => code.StartsWith(stem, StringComparison.OrdinalIgnoreCase))
        .Select(code => long.TryParse(code[stem.Length..], out var number) ? number : startingNumber - 1)
        .DefaultIfEmpty(startingNumber - 1)
        .Max() + 1;

    private static string BusinessCode(string stem, long sequence, int paddingWidth) => $"{stem}{sequence.ToString().PadLeft(paddingWidth, '0')}";

    private sealed record NotificationCodeFormat(string NotificationPrefix, string AnnouncementPrefix, string HistoryPrefix, string Separator, bool IncludeYear, int Year, long StartingNumber, int PaddingWidth)
    {
        public string Stem(string prefix) => IncludeYear ? $"{prefix}{Separator}{Year}{Separator}" : $"{prefix}{Separator}";
    }
}
