using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class GradeRecordConfiguration : IEntityTypeConfiguration<GradeRecord>
{
    public void Configure(EntityTypeBuilder<GradeRecord> builder)
    {
        builder.ToTable("GradeRecords", table =>
        {
            table.HasCheckConstraint(
                "CK_GradeRecords_ReviewStatus",
                "[ReviewStatus] IN (N'Pending', N'SubmissionRequested', N'SubmissionAuthorized', N'Submitted', N'Approved', N'Rejected', N'ResubmitRequested', N'ResubmitAuthorized')");
            table.HasCheckConstraint(
                "CK_GradeRecords_Scores",
                "[AttendanceScore] >= 0 AND [AttendanceMaximum] > 0 AND [AssignmentScore] >= 0 AND [AssignmentMaximum] > 0 AND [MidtermScore] >= 0 AND [MidtermMaximum] > 0 AND [FinalExamScore] >= 0 AND [FinalExamMaximum] > 0 AND [Score] >= 0");
        });
        builder.HasIndex(x => x.GradeCode).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.CourseId, x.AcademicYear, x.Term }).IsUnique();
        builder.HasIndex(x => new { x.StudentEnrollmentId, x.CourseId }).IsUnique();
        builder.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Term })
            .IncludeProperties(x => new { x.CourseId, x.Score, x.LetterGrade, x.ReviewStatus, x.FinalizedAtUtc });
        builder.HasIndex(x => new { x.CourseId, x.AcademicYear, x.Term, x.ReviewStatus })
            .IncludeProperties(x => new { x.StudentId, x.Score, x.FinalizedAtUtc });
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
        builder.Property(x => x.RowVersion).IsRowVersion();
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
        builder.HasOne(x => x.StudentEnrollment)
            .WithMany()
            .HasForeignKey(x => new { x.StudentEnrollmentId, x.StudentId })
            .HasPrincipalKey(x => new { x.Id, x.StudentId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SubmittedByTeacher)
            .WithMany()
            .HasForeignKey(x => x.SubmittedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
