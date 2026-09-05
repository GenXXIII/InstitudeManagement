using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.HasIndex(x => x.ClassroomCode).IsUnique();
        builder.HasIndex(x => x.DepartmentId);
        builder.Property(x => x.ClassroomCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Building).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RoomType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
