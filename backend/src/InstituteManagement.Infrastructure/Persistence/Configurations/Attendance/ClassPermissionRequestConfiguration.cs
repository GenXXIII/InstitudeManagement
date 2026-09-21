using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ClassPermissionRequestConfiguration : IEntityTypeConfiguration<ClassPermissionRequest>
{
    public void Configure(EntityTypeBuilder<ClassPermissionRequest> builder)
    {
        builder.ToTable("ClassPermissionRequests", "Attendance");
        builder.HasIndex(x => new { x.StudentId, x.SessionDate }).IsUnique();
        builder.HasIndex(x => new { x.SessionDate, x.Status });
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}
