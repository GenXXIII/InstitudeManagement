using InstituteManagement.Application.DTOs;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;
using static InstituteManagement.Infrastructure.Services.Record.OperationalRecordFields;

namespace InstituteManagement.Infrastructure.Services.Record;

public sealed class DepartmentOperationalRecordReader(InstituteDbContext db) : IOperationalRecordReader
{
    public string Module => "departments";

    public async Task<IReadOnlyList<OperationalRecordDto>> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var departments = await db.Departments.AsNoTracking().Include(x => x.HeadTeacher)
            .Where(x => !departmentId.HasValue || x.Id == departmentId)
            .OrderBy(x => x.DepartmentCode).ToListAsync(cancellationToken);
        var ids = departments.Select(x => x.Id).ToList();
        var students = await db.StudentEnrollments.AsNoTracking().Where(x => ids.Contains(x.DepartmentId)).ToListAsync(cancellationToken);
        var teachers = await db.TeacherAssignments.AsNoTracking().Where(x => x.DepartmentId.HasValue && ids.Contains(x.DepartmentId.Value)).ToListAsync(cancellationToken);
        var courses = await db.CourseAssignments.AsNoTracking().Include(x => x.Course).Where(x => ids.Contains(x.DepartmentId)).ToListAsync(cancellationToken);
        var classrooms = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Classroom).Where(x => x.DepartmentId == null || ids.Contains(x.DepartmentId.Value)).ToListAsync(cancellationToken);
        var sessions = await db.ClassSessionRecords.AsNoTracking().Where(x => ids.Contains(x.DepartmentId)).ToListAsync(cancellationToken);

        return departments.Select(department =>
        {
            var departmentStudents = students.Where(x => x.DepartmentId == department.Id).ToList();
            var departmentTeachers = teachers.Where(x => x.DepartmentId == department.Id).ToList();
            var departmentCourses = courses.Where(x => x.DepartmentId == department.Id).ToList();
            var departmentClassrooms = classrooms.Where(x => x.DepartmentId == null || x.DepartmentId == department.Id).ToList();
            var completed = sessions.Where(x => x.DepartmentId == department.Id).ToList();
            var periods = departmentStudents.Select(x => (x.AcademicYear, Term: x.Semester))
                .Concat(departmentTeachers.Select(x => (x.AcademicYear, Term: x.Semester)))
                .Concat(departmentCourses.Select(x => (x.AcademicYear, Term: x.Semester)))
                .Concat(departmentClassrooms.Select(x => (x.AcademicYear, Term: x.Semester)))
                .Concat(completed.Select(x => (x.AcademicYear, Term: x.Term))).Distinct().ToList();
            var overviewEvents = periods.Select(period =>
            {
                var periodStudents = departmentStudents.Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Term && x.Status == "Active").ToList();
                var periodTeachers = departmentTeachers.Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Term && x.Status != "Removed" && x.Status != "Unassigned").ToList();
                var periodCourses = departmentCourses.Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Term && x.Status == "Active").ToList();
                var periodRooms = departmentClassrooms.Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Term && x.Status != "Removed" && x.Status != "Unassigned").ToList();
                var periodSessions = completed.Where(x => x.AcademicYear == period.AcademicYear && x.Term == period.Term).ToList();
                var years = periodStudents.Select(x => $"Year {x.YearLevel}").Concat(periodCourses.Select(x => $"Year {x.YearLevel}")).Distinct().OrderBy(x => x).ToList();
                var timestamps = periodStudents.Select(x => x.UpdatedAtUtc).Concat(periodTeachers.Select(x => x.UpdatedAtUtc)).Concat(periodCourses.Select(x => x.UpdatedAtUtc)).Concat(periodRooms.Select(x => x.UpdatedAtUtc)).Concat(periodSessions.Select(x => x.UpdatedAtUtc)).ToList();
                var at = timestamps.Count == 0 ? department.UpdatedAtUtc : timestamps.Max();
                return (at, Create(
                    ("Activity", "Department semester"), ("Academic year", period.AcademicYear), ("Term", period.Term),
                    ("Date", at.ToString("yyyy-MM-dd")), ("Time", at.ToString("HH:mm")),
                    ("Department", department.Name), ("Head", department.HeadTeacher?.FullName ?? department.Head ?? "Not appointed"),
                    ("Year", years.Count == 0 ? "No year data" : string.Join(", ", years)),
                    ("Students", periodStudents.Select(x => x.StudentId).Distinct().Count().ToString()),
                    ("Teachers", periodTeachers.Select(x => x.TeacherId).Distinct().Count().ToString()),
                    ("Courses", periodCourses.Select(x => x.CourseId).Distinct().Count().ToString()),
                    ("Classrooms", periodRooms.Select(x => x.ClassroomId).Distinct().Count().ToString()),
                    ("Recorded periods", periodSessions.Count.ToString()),
                    ("Running classes", periodSessions.Count(x => TeacherPresence.IsPresent(x.TeacherAttendanceStatus)).ToString()),
                    ("Classes not held", periodSessions.Count(x => !TeacherPresence.IsPresent(x.TeacherAttendanceStatus)).ToString()),
                    ("Course names", string.Join("; ", periodCourses.Select(x => x.Course?.Name ?? "Course").Distinct().OrderBy(x => x)))));
            });
            var sessionEvents = completed.Select(x => (x.UpdatedAtUtc, Create(
                ("Activity", "Completed class"), ("Academic year", x.AcademicYear), ("Term", x.Term),
                ("Date", x.SessionDate.ToString("yyyy-MM-dd")), ("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"),
                ("Year", $"Year {x.YearLevel}"), ("Course", x.CourseName), ("Teacher", x.TeacherName),
                ("Classroom", x.ClassroomCode), ("Students", x.StudentCount.ToString()),
                ("Teacher attendance", x.TeacherAttendanceStatus), ("Session status", TeacherPresence.SessionStatus(x.TeacherAttendanceStatus)),
                ("Reason", TeacherPresence.Reason(x.TeacherAttendanceStatus)),
                ("Attendance", $"{x.PresentCount + x.LateCount} present · {x.AbsentCount} absent · {x.ExcusedCount} permission"))));
            var events = overviewEvents.Concat(sessionEvents).OrderByDescending(x => x.Item1).ToList();
            return new OperationalRecordDto(department.Id, "Department", department.Name,
                department.HeadTeacher?.FullName ?? department.Head ?? "Not appointed", department.IsActive ? "Active" : "Inactive",
                $"{events.Count} semester activities", events.Count == 0 ? null : events[0].Item1,
                events.Select(x => x.Item2).ToList(), Code: department.DepartmentCode, Department: department.Name, ResourceId: department.Id);
        }).ToList();
    }
}
