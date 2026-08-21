using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class AuditLogSeedFactory
{
    public static AuditLog[] Create(Student[] students, Course[] courses, Classroom[] rooms) => [new() { Type = "Attendance", Subject = students[0].StudentNumber, Action = "Present", Details = "Attendance recorded by ID card" }, new() { Type = "Grade", Subject = students[1].StudentNumber, Action = "Grade A", Details = "Mathematics result submitted" }, new() { Type = "Timetable", Subject = rooms[0].Code, Action = "Changed", Details = "Class moved to 10:00" }, new() { ResourceId = courses[1].Id, Type = "Course", Subject = courses[1].Name, Action = "Updated", Details = "Course capacity updated" }];
}
