using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasIndex(x => x.NotificationCode).IsUnique();
        builder.HasIndex(x => new { x.IsRead, x.CreateAt });
        builder.Property(x => x.NotificationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(32).IsRequired();
    }
}
