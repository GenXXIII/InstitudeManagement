using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class StudentPaymentConfiguration : IEntityTypeConfiguration<StudentPayment>
{
    public void Configure(EntityTypeBuilder<StudentPayment> builder)
    {
        builder.ToTable("StudentPayments", "Finance");
        builder.HasIndex(x => x.StudentEnrollmentId).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.AcademicYear, x.Semester, x.Status, x.DueOn });
        builder.HasIndex(x => new { x.PaymentCode, x.AcademicYear, x.Semester }).IsUnique();
        builder.Property(x => x.PaymentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AmountDue).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ConfirmationMethod).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.StudentEnrollment).WithOne().HasForeignKey<StudentPayment>(x => x.StudentEnrollmentId);
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
    }
}
