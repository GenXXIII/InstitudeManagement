using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class StudentEnrollmentConfiguration : IEntityTypeConfiguration<StudentEnrollment>
{
    public void Configure(EntityTypeBuilder<StudentEnrollment> builder)
    {
        builder.ToTable("StudentEnrollments", "Enrollment");
        builder.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.EnrollmentCode, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.AcademicYear, x.Semester, x.Status, x.Shift, x.DepartmentId, x.YearLevel })
            .IncludeProperties(x => new { x.StudentId, x.EnrollmentCode });
        builder.HasIndex(x => new { x.DepartmentId, x.YearLevel });
        builder.Property(x => x.EnrollmentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PublicId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.PublicId).IsUnique().HasFilter("[PublicId] <> ''");
        builder.Property(x => x.FinanceCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResultCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.FinanceCode).IsUnique().HasFilter("[FinanceCode] <> ''");
        builder.HasIndex(x => x.ResultCode).IsUnique().HasFilter("[ResultCode] <> ''");
        builder.Property(x => x.OperationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecordCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HistoryCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Shift).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
    }
}
