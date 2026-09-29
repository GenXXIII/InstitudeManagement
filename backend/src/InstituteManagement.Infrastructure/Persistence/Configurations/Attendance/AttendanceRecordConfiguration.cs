using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords", table =>
        {
            table.HasCheckConstraint("CK_AttendanceRecords_Status", "[Status] IN (N'Present', N'Late', N'Absent', N'Excused', N'Permission')");
        });
        builder.HasIndex(x => x.AttendanceCode).IsUnique();
        builder.HasIndex(x => new { x.StudentEnrollmentId, x.Date }).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Term, x.Date });
        builder.HasIndex(x => x.Date)
            .IncludeProperties(x => new { x.StudentId, x.Status, x.CheckedInAt });
        builder.HasIndex(x => new { x.AcademicYear, x.Term, x.CreateAt })
            .IsDescending(false, false, true);
        builder.Property(x => x.AttendanceCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Term).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.StudentEnrollment)
            .WithMany()
            .HasForeignKey(x => new { x.StudentEnrollmentId, x.StudentId })
            .HasPrincipalKey(x => new { x.Id, x.StudentId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
