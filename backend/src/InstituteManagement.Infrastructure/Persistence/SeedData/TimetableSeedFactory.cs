using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class TimetableSeedFactory
{
    public static IEnumerable<ScheduleEntry> Create(Course[] courses, Teacher[] teachers, Classroom[] rooms) => Enumerable.Range(1, 5).SelectMany(day => courses.Take(6).Select((course, index) => new ScheduleEntry { CourseId = course.Id, TeacherId = teachers[index].Id, ClassroomId = rooms[(index + day - 1) % rooms.Length].Id, DayOfWeek = (DayOfWeek)day, StartsAt = new TimeOnly(8 + index, 0), EndsAt = new TimeOnly(9 + index, 0), Status = "Upcoming" }));
}
