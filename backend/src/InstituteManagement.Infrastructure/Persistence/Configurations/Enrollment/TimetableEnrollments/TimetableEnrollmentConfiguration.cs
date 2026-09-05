using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class TimetableEnrollmentConfiguration : IEntityTypeConfiguration<TimetableEnrollment>
{
    public void Configure(EntityTypeBuilder<TimetableEnrollment> builder)
    {
        builder.ToTable("TimetableEnrollments", "Enrollment");
        builder.HasIndex(x => x.EnrollmentCode).IsUnique();
        builder.HasIndex(x => new { x.ScheduleEntryId, x.AcademicYear, x.Semester }).IsUnique();
        builder.Property(x => x.EnrollmentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.ScheduleEntry).WithMany().HasForeignKey(x => x.ScheduleEntryId);
    }
}
