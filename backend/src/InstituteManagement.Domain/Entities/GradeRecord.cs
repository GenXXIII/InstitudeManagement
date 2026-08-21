namespace InstituteManagement.Domain.Entities;

public sealed class GradeRecord : Entity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public decimal Score { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = "Semester 1";
}
