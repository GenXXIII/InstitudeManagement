using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class GradeRecordConfiguration : IEntityTypeConfiguration<GradeRecord>
{
    public void Configure(EntityTypeBuilder<GradeRecord> builder)
    {
        builder.HasIndex(x => x.GradeCode).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.CourseId, x.AcademicYear, x.Term }).IsUnique();
        builder.HasIndex(x => new { x.AcademicYear, x.Term });
        builder.HasIndex(x => x.UpdatedAtUtc).IncludeProperties(x => x.Score);
        builder.Property(x => x.GradeCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AttendanceScore).HasPrecision(5, 2);
        builder.Property(x => x.AttendanceMaximum).HasPrecision(5, 2);
        builder.Property(x => x.AssignmentScore).HasPrecision(5, 2);
        builder.Property(x => x.AssignmentMaximum).HasPrecision(5, 2);
        builder.Property(x => x.MidtermScore).HasPrecision(5, 2);
        builder.Property(x => x.MidtermMaximum).HasPrecision(5, 2);
        builder.Property(x => x.FinalExamScore).HasPrecision(5, 2);
        builder.Property(x => x.FinalExamMaximum).HasPrecision(5, 2);
        builder.Property(x => x.Score).HasPrecision(5, 2);
        builder.Property(x => x.LetterGrade).HasMaxLength(4).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Term).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ReviewStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReviewNote).HasMaxLength(500);
        builder.HasIndex(x => new { x.ReviewStatus, x.AcademicYear, x.Term });
        builder.HasIndex(x => new { x.FinalizedAtUtc, x.AcademicYear, x.Term });
        builder.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubmittedByTeacher)
            .WithMany()
            .HasForeignKey(x => x.SubmittedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
