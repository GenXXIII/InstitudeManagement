using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments", "Enrollment");
        builder.HasIndex(x => new { x.TeacherId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.EnrollmentCode, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.AcademicYear, x.Semester, x.Status, x.DepartmentId })
            .IncludeProperties(x => new { x.TeacherId, x.EnrollmentCode });
        builder.HasIndex(x => x.DepartmentId);
        builder.Property(x => x.EnrollmentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PublicId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.PublicId).IsUnique().HasFilter("[PublicId] <> ''");
        builder.Property(x => x.OperationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecordCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HistoryCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
    }
}
