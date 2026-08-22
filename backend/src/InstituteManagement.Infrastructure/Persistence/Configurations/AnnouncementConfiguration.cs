using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasIndex(x => x.AnnouncementCode).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.CreateAt });
        builder.HasOne(x => x.Notification).WithOne().HasForeignKey<Announcement>(x => x.NotificationId);
        builder.Property(x => x.AnnouncementCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
    }
}
