using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class GradeSeedFactory
{
    public static IEnumerable<GradeRecord> Create(Student[] students, Course[] courses) => students.Take(96).Select((student, index) => { var score = 68 + index % 29; return new GradeRecord { StudentId = student.Id, CourseId = courses[index % courses.Length].Id, Score = score, LetterGrade = score >= 90 ? "A" : score >= 80 ? "B" : score >= 70 ? "C" : score >= 60 ? "D" : "F" }; });
}
