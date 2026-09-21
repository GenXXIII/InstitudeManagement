using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstituteManagement.Infrastructure.Persistence.Configurations;

public sealed class SemesterResultPublicationConfiguration : IEntityTypeConfiguration<SemesterResultPublication>
{
    public void Configure(EntityTypeBuilder<SemesterResultPublication> builder)
    {
        builder.ToTable("SemesterResultPublications", "Grades");
        builder.HasIndex(x => new { x.StudentId, x.AcademicYear, x.Term }).IsUnique();
        builder.HasIndex(x => x.PublishedAtUtc);
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Term).HasMaxLength(64).IsRequired();
        builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
    }
}
