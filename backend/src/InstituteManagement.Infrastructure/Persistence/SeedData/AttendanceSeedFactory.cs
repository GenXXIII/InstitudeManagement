using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class AttendanceSeedFactory
{
    public static IEnumerable<AttendanceRecord> Create(Student[] students)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return students.Select((student, index) => new AttendanceRecord { StudentId = student.Id, Date = today, CheckedInAt = index % 12 == 0 ? new TimeOnly(8, 18) : new TimeOnly(7, 45).AddMinutes(index % 28), Status = index % 17 == 0 ? "Absent" : index % 12 == 0 ? "Late" : "Present" });
    }
}
