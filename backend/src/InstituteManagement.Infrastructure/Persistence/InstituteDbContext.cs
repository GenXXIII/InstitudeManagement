using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public sealed partial class InstituteDbContext(DbContextOptions<InstituteDbContext> options) : DbContext(options)
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
        var format = RequiresNotificationCodeFormat() ? LoadNotificationCodeFormat() : null;
        AssignSourceBusinessCodes(format);
        CaptureNotificationHistory();
        AssignHistoryBusinessCodes(format);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var format = RequiresNotificationCodeFormat() ? LoadNotificationCodeFormat() : null;
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
}
