using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class FinancialPaymentConfiguration : IEntityTypeConfiguration<FinancialPayment>
{
    public void Configure(EntityTypeBuilder<FinancialPayment> builder)
    {
        builder.ToTable("Payments", "Finance");
        builder.HasIndex(x => x.PaymentCode).IsUnique();
        builder.HasIndex(x => new { x.FinancialAccountId, x.Status, x.PaidAtUtc });
        builder.Property(x => x.PaymentCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Method).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.TransactionReference).HasMaxLength(256).IsRequired();
        builder.HasOne(x => x.FinancialAccount).WithMany(x => x.Payments).HasForeignKey(x => x.FinancialAccountId);
    }
}
