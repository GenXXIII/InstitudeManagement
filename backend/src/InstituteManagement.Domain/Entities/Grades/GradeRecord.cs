namespace InstituteManagement.Domain.Entities;

public sealed class GradeRecord : Entity
{
    public string GradeCode { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public decimal AttendanceScore { get; set; }
    public decimal AttendanceMaximum { get; set; } = 10;
    public decimal AssignmentScore { get; set; }
    public decimal AssignmentMaximum { get; set; } = 20;
    public decimal MidtermScore { get; set; }
    public decimal MidtermMaximum { get; set; } = 20;
    public decimal FinalExamScore { get; set; }
    public decimal FinalExamMaximum { get; set; } = 50;
    public decimal Score { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = "Semester 1";
    public Guid? SubmittedByTeacherId { get; set; }
    public Teacher? SubmittedByTeacher { get; set; }
    public string ReviewStatus { get; set; } = "Pending";
    public string ReviewNote { get; set; } = string.Empty;
    public int SubmissionVersion { get; set; } = 1;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
