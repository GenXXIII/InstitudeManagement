using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class ScheduleEntryConfiguration : IEntityTypeConfiguration<ScheduleEntry>
{
    public void Configure(EntityTypeBuilder<ScheduleEntry> builder)
    {
        builder.HasIndex(x => x.TimetableCode).IsUnique();
        builder.HasIndex(x => new { x.DayOfWeek, x.StartsAt, x.EndsAt });
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.ClassroomId);
        builder.HasIndex(x => x.TeacherId);
        builder.Property(x => x.TimetableCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.YearLevel).HasDefaultValue(1).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Classroom)
            .WithMany()
            .HasForeignKey(x => x.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
