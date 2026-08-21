using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasIndex(x => new { x.Section, x.Key }).IsUnique();
        builder.Property(x => x.Section).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(2048).IsRequired();
    }
}
