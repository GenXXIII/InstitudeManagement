using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(x => x.ResourceId);
        builder.HasIndex(x => new { x.Type, x.CreatedAtUtc });
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
    }
}
