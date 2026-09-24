using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("FinancialAccounts", "Finance");
        builder.HasIndex(x => x.StudentEnrollmentId).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Semester }).IsUnique();
        builder.HasIndex(x => new { x.AcademicYear, x.Semester, x.Status, x.ClosedAtUtc, x.DueOn });
        builder.HasIndex(x => new { x.FinancialAccountCode, x.AcademicYear, x.Semester }).IsUnique();
        builder.Property(x => x.FinancialAccountCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Semester).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PaymentPlan).HasMaxLength(16).IsRequired();
        builder.Property(x => x.DeclaredAmount).HasPrecision(18, 2);
        builder.Property(x => x.BakongQrPayload).HasMaxLength(4096).IsRequired();
        builder.Property(x => x.BakongMd5).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TuitionFee).HasPrecision(18, 2);
        builder.Property(x => x.OtherFee).HasPrecision(18, 2);
        builder.Property(x => x.AdjustmentAmount).HasPrecision(18, 2);
        builder.Property(x => x.AdjustmentReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LatePenaltyAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.StudentEnrollment).WithOne().HasForeignKey<FinancialAccount>(x => x.StudentEnrollmentId);
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
    }
}
