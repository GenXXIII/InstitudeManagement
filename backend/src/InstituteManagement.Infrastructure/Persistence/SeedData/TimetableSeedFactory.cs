using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class TimetableSeedFactory
{
    public static IEnumerable<ScheduleEntry> Create(Course[] courses, Classroom[] rooms) =>
        Enumerable.Range(1, 7).SelectMany(dayNumber =>
        {
            var day = (DayOfWeek)(dayNumber % 7);
            var periods = AcademicTimetablePolicy.ForDay(day);
            return periods.SelectMany((period, periodIndex) => rooms.Select((room, roomIndex) =>
                {
                    var departmentCourses = courses.Where(course => course.DepartmentId == room.DepartmentId).ToArray();
                    var departmentRoomIndex = rooms.Where(candidate => candidate.DepartmentId == room.DepartmentId).TakeWhile(candidate => candidate.Id != room.Id).Count();
                    var course = departmentCourses[departmentRoomIndex % departmentCourses.Length];
                    return new ScheduleEntry
                    {
                        CourseId = course.Id,
                        TeacherId = course.TeacherId!.Value,
                        ClassroomId = room.Id,
                        YearLevel = ((roomIndex + periodIndex) % 4) + 1,
                        DayOfWeek = day,
                        StartsAt = period.StartsAt,
                        EndsAt = period.EndsAt,
                        Status = "Upcoming"
                    };
                }));
        });
}
