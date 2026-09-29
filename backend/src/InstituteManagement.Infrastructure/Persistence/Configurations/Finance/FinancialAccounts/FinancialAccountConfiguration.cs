using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("FinancialAccounts", "Finance", table =>
        {
            table.HasCheckConstraint("CK_FinancialAccounts_Status", "[Status] IN (N'Pending', N'Partial', N'Paid', N'Refunded', N'Cancelled')");
            table.HasCheckConstraint("CK_FinancialAccounts_Amounts", "[TuitionFee] >= 0 AND [OtherFee] >= 0 AND [LatePenaltyDays] >= 0 AND [LatePenaltyAmount] >= 0 AND ([DeclaredAmount] IS NULL OR [DeclaredAmount] >= 0)");
            table.HasCheckConstraint("CK_FinancialAccounts_Closure", "[ClosedAtUtc] IS NULL OR [Status] = N'Paid'");
        });
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
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne(x => x.StudentEnrollment)
            .WithOne()
            .HasForeignKey<FinancialAccount>(x => new { x.StudentEnrollmentId, x.StudentId })
            .HasPrincipalKey<StudentEnrollment>(x => new { x.Id, x.StudentId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
    }
}
