using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistory>
{
    public void Configure(EntityTypeBuilder<NotificationHistory> builder)
    {
        builder.HasIndex(x => x.NotificationHistoryCode).IsUnique();
        builder.HasIndex(x => new { x.Kind, x.CreateAt });
        builder.HasIndex(x => x.SourceId);
        builder.Property(x => x.NotificationHistoryCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(32).IsRequired();
    }
}
