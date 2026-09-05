using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments", "Enrollment");
        builder.HasIndex(x => x.EnrollmentCode).IsUnique();
        builder.HasIndex(x => new { x.TeacherId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => x.DepartmentId);
        builder.Property(x => x.EnrollmentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
    }
}
