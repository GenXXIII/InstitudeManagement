using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(x => x.AuditLogCode).IsUnique();
        builder.HasIndex(x => x.ResourceId);
        builder.HasIndex(x => new { x.Type, x.CreateAt });
        builder.HasIndex(x => x.CreateAt)
            .IsDescending()
            .IncludeProperties(x => new { x.Action, x.Subject, x.Type });
        builder.HasIndex(x => new { x.Type, x.Action, x.ResourceId })
            .HasFilter("[ResourceId] IS NOT NULL");
        builder.Property(x => x.AuditLogCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
    }
}
