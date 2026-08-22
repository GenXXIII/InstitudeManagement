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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CaptureNotificationHistory();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        CaptureNotificationHistory();
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
}
