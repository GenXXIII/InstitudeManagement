using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ClassroomAssignmentConfiguration : IEntityTypeConfiguration<ClassroomAssignment>
{
    public void Configure(EntityTypeBuilder<ClassroomAssignment> builder)
    {
        builder.ToTable("ClassroomAssignments", "Enrollment");
        builder.HasIndex(x => new { x.ClassroomId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => x.DepartmentId);
        builder.Property(x => x.Access).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
    }
}
