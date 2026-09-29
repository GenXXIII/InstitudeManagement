using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class StudentAcademicEnrollmentConfiguration : IEntityTypeConfiguration<StudentAcademicEnrollment>
{
    public void Configure(EntityTypeBuilder<StudentAcademicEnrollment> builder)
    {
        builder.ToTable("StudentAcademicEnrollments", "Enrollment", table =>
        {
            table.HasCheckConstraint(
                "CK_StudentAcademicEnrollments_Status",
                "[Status] IN (N'Active', N'Completed', N'Cancelled')");
            table.HasCheckConstraint(
                "CK_StudentAcademicEnrollments_Completion",
                "([Status] = N'Active' AND [CompletedAtUtc] IS NULL) OR ([Status] <> N'Active' AND [CompletedAtUtc] IS NOT NULL)");
        });
        builder.HasIndex(x => x.EnrollmentCode).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.Status })
            .IncludeProperties(x => new { x.EnrollmentCode, x.StartedAtUtc, x.CompletedAtUtc });
        builder.Property(x => x.EnrollmentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
