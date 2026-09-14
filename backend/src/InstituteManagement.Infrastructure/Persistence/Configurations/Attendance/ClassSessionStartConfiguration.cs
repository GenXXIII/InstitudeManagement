using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ClassSessionStartConfiguration : IEntityTypeConfiguration<ClassSessionStart>
{
    public void Configure(EntityTypeBuilder<ClassSessionStart> builder)
    {
        builder.ToTable("ClassSessionStarts", "Attendance");
        builder.HasIndex(x => new { x.ScheduleEntryId, x.SessionDate }).IsUnique();
        builder.HasIndex(x => new { x.TeacherId, x.SessionDate });
        builder.HasIndex(x => x.StartedAtUtc);
        builder.HasOne(x => x.ScheduleEntry).WithMany().HasForeignKey(x => x.ScheduleEntryId);
        builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
    }
}
