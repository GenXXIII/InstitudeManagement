using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class CourseAssignmentConfiguration : IEntityTypeConfiguration<CourseAssignment>
{
    public void Configure(EntityTypeBuilder<CourseAssignment> builder)
    {
        builder.ToTable("CourseAssignments", "Enrollment");
        builder.HasIndex(x => new { x.CourseId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.DepartmentId, x.YearLevel });
        builder.HasIndex(x => x.TeacherId);
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
    }
}
