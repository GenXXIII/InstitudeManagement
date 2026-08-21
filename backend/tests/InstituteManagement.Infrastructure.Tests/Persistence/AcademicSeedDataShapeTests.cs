using InstituteManagement.Infrastructure.Persistence.SeedData;
using InstituteManagement.Domain.Timetables;

namespace InstituteManagement.Infrastructure.Tests.Persistence;

public sealed class AcademicSeedDataShapeTests
{
    [Fact]
    public void Attendance_seed_contains_a_five_record_timeline_per_student()
    {
        var departments = DepartmentSeedFactory.Create();
        var students = StudentSeedFactory.Create(departments);

        var records = AttendanceSeedFactory.Create(students).ToList();

        Assert.All(records.GroupBy(record => record.StudentId), group => Assert.Equal(5, group.Count()));
        Assert.Contains(records, record => record.Status == "Absent" && record.CheckedInAt == new TimeOnly(11, 30));
    }

    [Fact]
    public void Grade_seed_contains_every_department_course_per_student()
    {
        var departments = DepartmentSeedFactory.Create();
        var teachers = TeacherSeedFactory.Create(departments);
        var students = StudentSeedFactory.Create(departments);
        var courses = CourseSeedFactory.Create(departments, teachers);

        var records = GradeSeedFactory.Create(students, courses).ToList();

        Assert.All(students.Take(96), student =>
            Assert.Equal(courses.Count(course => course.DepartmentId == student.DepartmentId), records.Count(record => record.StudentId == student.Id)));
        Assert.All(records, record => Assert.Equal(students.First(student => student.Id == record.StudentId).DepartmentId, courses.First(course => course.Id == record.CourseId).DepartmentId));
    }

    [Fact]
    public void Timetable_seed_supports_thirteen_concurrent_rooms_and_four_years()
    {
        var departments = DepartmentSeedFactory.Create();
        var teachers = TeacherSeedFactory.Create(departments);
        var courses = CourseSeedFactory.Create(departments, teachers);
        var rooms = ClassroomSeedFactory.Create(departments);

        var schedules = TimetableSeedFactory.Create(courses, rooms).ToList();
        var firstPeriod = AcademicTimetablePolicy.ForDay(DayOfWeek.Monday)[0];
        var concurrent = schedules.Where(entry => entry.DayOfWeek == DayOfWeek.Monday && entry.StartsAt == firstPeriod.StartsAt && entry.EndsAt == firstPeriod.EndsAt).ToList();

        Assert.Equal(13, rooms.Length);
        Assert.Single(rooms, room => room.RoomType == "Meeting Room");
        Assert.Equal(13, concurrent.Count);
        Assert.Equal(13, concurrent.Select(entry => entry.ClassroomId).Distinct().Count());
        Assert.Equal(13, concurrent.Select(entry => entry.TeacherId).Distinct().Count());
        Assert.Equal([1, 2, 3, 4], concurrent.Select(entry => entry.YearLevel).Distinct().Order().ToArray());
    }
}
