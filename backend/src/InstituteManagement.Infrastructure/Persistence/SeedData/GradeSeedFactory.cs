using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class GradeSeedFactory
{
    public static IEnumerable<GradeRecord> Create(Student[] students, Course[] courses, string academicYear = "2026\u20132027", string term = "Semester 1") =>
        students.Take(96).SelectMany((student, studentIndex) => courses
            .Where(course => course.DepartmentId == student.DepartmentId)
            .Select((course, courseIndex) =>
            {
                var score = 39 + ((studentIndex * 11 + courseIndex * 17) % 62);
                return new GradeRecord
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    Score = score,
                    LetterGrade = score >= 90 ? "A" : score >= 80 ? "B" : score >= 70 ? "C" : score >= 60 ? "D" : score >= 50 ? "E" : "F",
                    AcademicYear = academicYear,
                    Term = term
                };
            }));
}
