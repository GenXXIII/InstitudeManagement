using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class GradeRecordConfiguration : IEntityTypeConfiguration<GradeRecord>
{
    public void Configure(EntityTypeBuilder<GradeRecord> builder)
    {
        builder.HasIndex(x => new { x.StudentId, x.CourseId, x.AcademicYear, x.Term }).IsUnique();
        builder.Property(x => x.Score).HasPrecision(5, 2);
        builder.Property(x => x.LetterGrade).HasMaxLength(4).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Term).HasMaxLength(64).IsRequired();
        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
