using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasIndex(x => x.DepartmentCode).IsUnique();
        builder.Property(x => x.DepartmentCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Head).HasMaxLength(200).IsRequired();
        builder.HasOne(x => x.HeadTeacher)
            .WithMany()
            .HasForeignKey(x => x.HeadTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
