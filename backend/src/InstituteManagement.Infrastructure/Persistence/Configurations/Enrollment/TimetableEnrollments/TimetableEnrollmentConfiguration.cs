using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class TimetableEnrollmentConfiguration : IEntityTypeConfiguration<TimetableEnrollment>
{
    public void Configure(EntityTypeBuilder<TimetableEnrollment> builder)
    {
        builder.ToTable("TimetableEnrollments", "Enrollment");
        builder.HasIndex(x => new { x.ScheduleEntryId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.EnrollmentCode, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.AcademicYear, x.Semester, x.Status })
            .IncludeProperties(x => new { x.ScheduleEntryId, x.EnrollmentCode });
        builder.Property(x => x.EnrollmentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.YearLevel).HasDefaultValue(1).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.ScheduleEntry).WithMany().HasForeignKey(x => x.ScheduleEntryId);
        builder.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
        builder.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId);
    }
}
