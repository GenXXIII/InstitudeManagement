using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasIndex(x => x.AttendanceCode).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.Date }).IsUnique();
        builder.Property(x => x.AttendanceCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Term).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
