using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class AttendanceSeedFactory
{
    public static IEnumerable<AttendanceRecord> Create(Student[] students, string academicYear = "2026\u20132027", string term = "Semester 1")
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return students.SelectMany((student, studentIndex) => Enumerable.Range(0, 5).Select(dayOffset =>
        {
            var absent = (studentIndex + dayOffset) % 17 == 0;
            var late = !absent && (studentIndex + dayOffset) % 12 == 0;
            return new AttendanceRecord
            {
                StudentId = student.Id,
                Date = today.AddDays(-dayOffset),
                CheckedInAt = absent ? new TimeOnly(11, 30) : late ? new TimeOnly(8, 18) : new TimeOnly(7, 45).AddMinutes((studentIndex + dayOffset) % 28),
                Status = absent ? "Absent" : late ? "Late" : "Present",
                Method = absent ? "Manual" : "ID Card",
                AcademicYear = academicYear,
                Term = term
            };
        }));
    }
}
