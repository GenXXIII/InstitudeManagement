using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ClassSessionRecordConfiguration : IEntityTypeConfiguration<ClassSessionRecord>
{
    public void Configure(EntityTypeBuilder<ClassSessionRecord> builder)
    {
        builder.HasIndex(x => x.ClassSessionRecordCode).IsUnique();
        builder.HasIndex(x => new { x.ScheduleEntryId, x.SessionDate }).IsUnique();
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.TeacherId);
        builder.HasIndex(x => x.ClassroomId);
        builder.Property(x => x.ClassSessionRecordCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Term).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CourseName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TeacherName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TeacherAttendanceStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ClassroomCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.StudentAttendanceJson).IsRequired();
        builder.HasOne(x => x.ScheduleEntry).WithMany().HasForeignKey(x => x.ScheduleEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);
    }
}
