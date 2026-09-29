using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ClassSessionStudentAttendanceConfiguration : IEntityTypeConfiguration<ClassSessionStudentAttendance>
{
    public void Configure(EntityTypeBuilder<ClassSessionStudentAttendance> builder)
    {
        builder.ToTable("ClassSessionStudentAttendance", "Record", table =>
        {
            table.HasCheckConstraint(
                "CK_ClassSessionStudentAttendance_Status",
                "[Status] IN (N'Present', N'Late', N'Absent', N'Excused', N'Permission', N'Class not held', N'Not recorded')");
        });
        builder.HasIndex(x => new { x.ClassSessionRecordId, x.StudentId }).IsUnique();
        builder.HasIndex(x => new { x.StudentEnrollmentId, x.ClassSessionRecordId })
            .IncludeProperties(x => new { x.StudentId, x.Status, x.CheckedInAt });
        builder.HasIndex(x => new { x.StudentId, x.ClassSessionRecordId })
            .IncludeProperties(x => new { x.Status, x.CheckedInAt });
        builder.Property(x => x.StudentCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.StudentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CheckedInAt).HasMaxLength(5).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.ClassSessionRecord)
            .WithMany(x => x.StudentAttendance)
            .HasForeignKey(x => x.ClassSessionRecordId)
            .OnDelete(DeleteBehavior.Restrict);
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
