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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Student>().HasIndex(x => x.StudentNumber).IsUnique();
        modelBuilder.Entity<Teacher>().HasIndex(x => x.TeacherNumber).IsUnique();
        modelBuilder.Entity<Classroom>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<SystemSetting>().HasIndex(x => new { x.Section, x.Key }).IsUnique();
        modelBuilder.Entity<GradeRecord>().Property(x => x.Score).HasPrecision(5, 2);
        modelBuilder.Entity<Department>()
            .HasOne(x => x.HeadTeacher)
            .WithMany()
            .HasForeignKey(x => x.HeadTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // SQL Server rejects the model's legitimate relationship graph when
        // multiple cascade paths converge on attendance, grade, and schedule
        // records. Deletion is explicit in application workflows instead.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
