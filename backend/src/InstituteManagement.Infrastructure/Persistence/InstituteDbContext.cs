using InstituteManagement.Application.Common.Exceptions;
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
    public DbSet<ClassSessionStart> ClassSessionStarts => Set<ClassSessionStart>();
    public DbSet<ClassPermissionRequest> ClassPermissionRequests => Set<ClassPermissionRequest>();
    public DbSet<GradeRecord> GradeRecords => Set<GradeRecord>();
    public DbSet<SemesterResultPublication> SemesterResultPublications => Set<SemesterResultPublication>();
    public DbSet<ClassSessionRecord> ClassSessionRecords => Set<ClassSessionRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<StudentAcademicEnrollment> StudentAcademicEnrollments => Set<StudentAcademicEnrollment>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();
    public DbSet<ClassroomAssignment> ClassroomAssignments => Set<ClassroomAssignment>();
    public DbSet<TimetableEnrollment> TimetableEnrollments => Set<TimetableEnrollment>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<FinancialPayment> FinancialPayments => Set<FinancialPayment>();
    public DbSet<ClassSessionStudentAttendance> ClassSessionStudentAttendance => Set<ClassSessionStudentAttendance>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        var format = RequiresNotificationCodeFormat() ? LoadNotificationCodeFormat() : null;
        AssignSourceBusinessCodes(format);
        CaptureNotificationHistory();
        format ??= RequiresNotificationCodeFormat() ? LoadNotificationCodeFormat() : null;
        AssignHistoryBusinessCodes(format);
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException exception)
        {
            throw new PersistenceConflictException(exception);
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        var format = RequiresNotificationCodeFormat() ? LoadNotificationCodeFormat() : null;
        AssignSourceBusinessCodes(format);
        CaptureNotificationHistory();
        format ??= RequiresNotificationCodeFormat() ? LoadNotificationCodeFormat() : null;
        AssignHistoryBusinessCodes(format);
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new PersistenceConflictException(exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InstituteDbContext).Assembly);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            entityType.FindProperty(nameof(Entity.CreateAt))?.SetColumnName("CreatedAtUtc");
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreateAt == default) entry.Entity.CreateAt = now;
                if (entry.Entity.UpdatedAtUtc == default) entry.Entity.UpdatedAtUtc = entry.Entity.CreateAt;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
